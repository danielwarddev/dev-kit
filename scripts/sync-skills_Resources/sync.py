import argparse
from pathlib import Path
import shutil
import tempfile


FILES = ("AGENTS.md", ".editorconfig")


def reject_links(path):
    for entry in (path, *path.parents):
        if entry.is_symlink() or getattr(entry, "is_junction", lambda: False)():
            raise ValueError(f"Linked paths are not supported: {entry}")


def transfer(source_skills, source_config, destination_skills, destination_config):
    paths = (source_skills, source_config, destination_skills, destination_config)
    for path in paths:
        reject_links(path)
    source_skills, source_config, destination_skills, destination_config = (
        path.resolve() for path in paths
    )
    for source in (source_skills, source_config):
        for destination in (destination_skills, destination_config):
            if source == destination or source in destination.parents or destination in source.parents:
                raise ValueError("Source and destination must not overlap.")
    if not source_skills.is_dir():
        raise ValueError(f"Missing skills directory: {source_skills}")
    skills = sorted(
        path for path in source_skills.iterdir()
        if path.is_dir() and (path / "SKILL.md").is_file()
    )
    if not skills:
        raise ValueError(f"No skills found: {source_skills}")
    for skill in skills:
        for entry in (skill, *skill.rglob("*")):
            reject_links(entry)
        target = destination_skills / skill.name
        reject_links(target)
        if target.exists() and not target.is_dir():
            raise ValueError(f"Skill destination is not a directory: {target}")
    for name in FILES:
        source = source_config / name
        target = destination_config / name
        reject_links(source)
        reject_links(target)
        if not source.is_file():
            raise ValueError(f"Missing source file: {source}")
        if target.exists() and not target.is_file():
            raise ValueError(f"File destination is not a file: {target}")

    destination_skills.mkdir(parents=True, exist_ok=True)
    destination_config.mkdir(parents=True, exist_ok=True)
    ignore = shutil.ignore_patterns(
        "__pycache__", "*.pyc", "*.pyo", ".git", "settings.local.json",
    )
    with tempfile.TemporaryDirectory(
        prefix=".sync-skills-", dir=destination_skills.parent,
    ) as directory:
        stage = Path(directory)
        staged_skills = stage / "skills"
        staged_skills.mkdir()
        for skill in skills:
            shutil.copytree(skill, staged_skills / skill.name, ignore=ignore)
        for name in FILES:
            shutil.copy2(source_config / name, stage / name)
        replacements = [
            (staged_skills / skill.name, destination_skills / skill.name)
            for skill in skills
        ] + [(stage / name, destination_config / name) for name in FILES]
        completed = []
        try:
            for index, (prepared, target) in enumerate(replacements):
                backup = stage / f"previous-{index}"
                existed = target.exists()
                if existed:
                    target.rename(backup)
                completed.append((prepared, target, backup, existed))
                prepared.rename(target)
        except OSError:
            for prepared, target, backup, existed in reversed(completed):
                if target.exists():
                    target.rename(prepared)
                if existed:
                    backup.rename(target)
            raise
    print(f"Copied {len(skills)} skills to {destination_skills}")
    print(f"Copied AGENTS.md and .editorconfig to {destination_config}")


def main():
    parser = argparse.ArgumentParser(
        description="Replace matching skills, AGENTS.md, and .editorconfig; leave unrelated skills alone.",
    )
    commands = parser.add_subparsers(dest="direction", required=True)
    copy = commands.add_parser("copy", prog="pull-project-files")
    copy.add_argument("kit", help=argparse.SUPPRESS)
    copy.add_argument("project", nargs="?", default=".")
    send = commands.add_parser("send", prog="push-project-files")
    send.add_argument("kit", help=argparse.SUPPRESS)
    send.add_argument("repo", help="Destination dev-kit Git checkout")
    send.add_argument("project", nargs="?", default=".")
    args = parser.parse_args()
    try:
        project = Path(args.project).absolute()
        if not project.is_dir():
            raise ValueError(f"Project directory does not exist: {project}")
        if args.direction == "copy":
            kit = Path(args.kit).absolute()
            transfer(kit / "skills", kit / "config", project / ".claude" / "skills", project)
        else:
            repo = Path(args.repo).absolute()
            if not (repo / ".git").exists() or not (repo / "skills").is_dir() or not (repo / "config").is_dir():
                raise ValueError(f"Expected a dev-kit Git checkout: {repo}")
            transfer(project / ".claude" / "skills", project, repo / "skills", repo / "config")
    except (ValueError, OSError) as error:
        parser.exit(1, f"ERROR: {error}\n")


if __name__ == "__main__":
    main()
