import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from operations import list_projects, operate, project_info, run_engine


@unittest.skipUnless(os.environ.get("GODOT_PATH"), "Set GODOT_PATH to run integration checks")
class OperationTests(unittest.TestCase):
    def test_resource_workflow_and_run_capture(self):
        with tempfile.TemporaryDirectory(prefix="godot-skill-") as directory:
            project = Path(directory)
            (project / "project.godot").write_text(
                'config_version=5\n[application]\nconfig/name="SkillSmoke"\n'
                '[rendering]\nrenderer/rendering_method="gl_compatibility"\n',
                encoding="utf-8",
            )
            operate(project, "create_scene", {"scene_path": "Scenes/Example.tscn"})
            operate(project, "add_node", {
                "scene_path": "Scenes/Example.tscn", "node_type": "Sprite2D",
                "node_name": "Preview", "properties": {"z_index": 2},
            })
            (project / "fixture.gd").write_text(
                'extends SceneTree\nfunc _init():\n'
                '\tvar image = Image.create(2, 2, false, Image.FORMAT_RGBA8)\n'
                '\timage.fill(Color.RED)\n\timage.save_png("res://preview.png")\n'
                '\tvar root = Node3D.new()\n\troot.name = "Tiles"\n'
                '\tvar tile = MeshInstance3D.new()\n\ttile.name = "Floor"\n'
                '\ttile.mesh = BoxMesh.new()\n\troot.add_child(tile)\n\ttile.owner = root\n'
                '\tvar scene = PackedScene.new()\n\tscene.pack(root)\n'
                '\tResourceSaver.save(scene, "res://Tiles.tscn")\n\troot.free()\n\tquit()\n',
                encoding="utf-8",
            )
            run_engine(["--headless", "--path", project, "--script", "res://fixture.gd"])
            run_engine(["--headless", "--editor", "--path", project, "--import"])
            operate(project, "load_sprite", {
                "scene_path": "Scenes/Example.tscn", "node_path": "Preview",
                "texture_path": "preview.png",
            })
            operate(project, "save_scene", {
                "scene_path": "Scenes/Example.tscn", "new_path": "Scenes/Copy.tscn",
            })
            operate(project, "export_mesh_library", {
                "scene_path": "Tiles.tscn", "output_path": "Tiles.tres",
                "mesh_item_names": ["Floor"],
            })
            uid = operate(project, "get_uid", {"file_path": "fixture.gd"})
            self.assertTrue(uid["exists"])
            self.assertTrue(uid["uid"].startswith("uid://"))
            operate(project, "resave_resources", {"project_path": "Scenes"})
            self.assertEqual(list_projects(project), [str(project)])
            self.assertEqual(project_info(project)["counts"]["scenes"], 3)
            (project / "inspect.gd").write_text(
                'extends SceneTree\nfunc _init():\n'
                '\tvar root = load("res://Scenes/Copy.tscn").instantiate()\n'
                '\tassert(root.get_node("Preview").z_index == 2)\n'
                '\tassert(root.get_node("Preview").texture.get_width() == 2)\n'
                '\tassert(load("res://Tiles.tres").get_item_list().size() == 1)\n'
                '\troot.free()\n\tprint("RESOURCE_CHECK_OK")\n\tquit()\n',
                encoding="utf-8",
            )
            self.assertIn("RESOURCE_CHECK_OK", run_engine(
                ["--headless", "--path", project, "--script", "res://inspect.gd"]
            ))
            process = subprocess.Popen(
                [os.environ["GODOT_PATH"], "--headless", "--debug", "--path",
                 str(project), "res://Scenes/Copy.tscn", "--quit-after", "3"],
                stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, encoding="utf-8",
            )
            stdout, stderr = process.communicate(timeout=30)
            self.assertEqual(process.returncode, 0, stdout + stderr)
            self.assertFalse(stderr.strip(), stderr)
            self.assertIn("Godot Engine", stdout)
            with open(project / "owned-run.log", "w", encoding="utf-8") as log:
                owned = subprocess.Popen(
                    [os.environ["GODOT_PATH"], "--headless", "--debug", "--path",
                     str(project), "res://Scenes/Copy.tscn"],
                    stdout=log, stderr=log,
                )
                try:
                    import time
                    time.sleep(1)
                    self.assertIsNone(owned.poll())
                    self.assertIn("Godot Engine", (project / "owned-run.log").read_text(encoding="utf-8"))
                finally:
                    owned.terminate()
                    owned.wait(timeout=10)
            with self.assertRaises(RuntimeError):
                operate(project, "load_sprite", {
                    "scene_path": "Scenes/Copy.tscn", "node_path": "NOT_PRESENT",
                    "texture_path": "preview.png",
                }, timeout=10)
            with self.assertRaises(ValueError):
                operate(project, "create_scene", {"scene_path": "../outside.tscn"})


if __name__ == "__main__":
    unittest.main()
