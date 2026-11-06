# Vehicles and driving scenes

Cars are the running example; adapt the same construction, lighting, and force
checks to other vehicles. When vehicles belong to a different genre from the
project's existing art, establish a separate profile rather than reusing the
existing palette, geometry, or artwork, and keep the existing profile intact.

## Readable vehicle construction

Choose the camera first. A top-down car needs a distinct front/rear, believable
wheelbase, attached tyres, body shell/fenders, bonnet, cabin/glass, and applicable
rear window/spoiler. A side-view car needs a different construction; do not
flatten one camera's artwork into another.

Block two or three inexpensive silhouette options before detailing. Equipment,
mirrors, wheels, and wings attach to the chassis rather than floating beside it.
Use roof/body planes, quiet glass masses, and selective paint/metal highlights.
Do not make every surface equally reflective or use a familiar branded design
unless appropriate rights and the task allow it.

Check native size and representative road backgrounds. Strong race markings and
lights can identify the vehicle, but should not replace a sound silhouette.
Trackside texture generally stays quieter than the vehicle.

## Directional art and world light

Keep a consistent center/anchor and pixel density across headings. Transparent
margins may grow to accommodate rotation; do not clip the car or resize it between
directions. Directional samples are not automatically a temporal animation or a
universal mandatory direction count.

Keep the world light direction consistent. Highlights on turning side planes
may change with their orientation; do not rotate a painted "upper-left" highlight
as though it were a decal. Limit incidental pixel crawling and abrupt lighting
flips while retaining intended surface turns.

Front-wheel geometry and rim details should rotate together when steering.
Rear wheels and contact locations remain connected to the chassis. A body/load
shift is distinct from moving the whole vehicle above the road.

## Cornering, inertia, and settling

The nose direction and trajectory can differ during a slide. Show steering
initiating a turn, momentum carrying the vehicle along its path, and appropriate
countersteering/braking/settling. Do not imply cornering by rotating a stationary
sprite alone. The visible body and wheels should explain the force story.

A top-down load shift can use restrained chassis displacement or changing visible
planes relative to contacts; do not add arbitrary vertical bobbing or detach tyres.
Skid marks and particles reinforce readable motion, not conceal a bad path.
Loop boundaries must continue heading, position, and motion rather than teleport.

## Track and obstacle checks

Establish broad navigable tarmac, consistent road width and continuous edges/kerbs
before repairs, markings, or paddock details. Markings follow the course rather
than resetting conspicuously at tile borders. Separate intentional obstacles
from the driving surface.

Check the vehicle's tyre contacts and swept shape around turns. Check the actual
opaque footprints of trackside props, not just their center points. Shadows may
reach the road without placing the solid barrier or cone there.

Inspect a native scene and real timed playback. Metadata, bounds, fixed wheelbase,
loop continuity, and alpha checks are necessary mechanical evidence, not proof
that driving looks convincing or that a playable physics controller exists.
Save source/preview outputs outside the skill and keep art-demo parameters
separate from game movement tuning.
