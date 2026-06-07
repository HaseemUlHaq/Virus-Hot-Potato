# The Virus Lab

*A mixed reality experience built on teamwork, problem-solving and fun.*

<img width="297" height="421" alt="Poster" src="https://github.com/user-attachments/assets/84f87e7f-77ef-49fd-bc02-ad352c05a7d3" />

## Introduction
The Virus Lab is a collaborative mixed reality experience designed for 3 players using Meta Quest headsets. Players take on the role of virus researchers in a shared lab environment that has been contaminated. To decontaminate the room and stop the virus from spreading, they must work together to replicate virus structures by combining their unique abilities.

The experience is built around a central challenge: a target virus formation is displayed in the play area, and players must study it, then collaboratively modify a set of basic virus objects to match the target. Each player controls a single property of the virus (such as colour, scale or pulsation), so no one can solve the puzzle alone. Success requires communication, observation, and coordination.

The project is designed with educational value in mind. It introduces concepts from real virology (virus structure, mutation, replication) through an accessible, hands-on format aimed at younger audiences. The goal is to make science feel tangible and collaborative as well as entertaining. Players experience how real research teams work: each specialist contributes one piece of the puzzle, and only together can they reach a solution.

The game runs in mixed reality, blending virtual virus objects and UI elements with the physical environment (a real table that players stand around). This grounds the experience in shared physical space, making collaboration feel natural and immediate.

### Repository
https://github.com/HaseemUlHaq/Virus-Hot-Potato.git

## Design Process

The following section will walk through the experience design process. This includes the concept evolution, design goals, design decisions, justifications, and challenges encountered during the development of the experience.

### Concept Evaluation
The concept went through three main phases, each shaped by a bodystorming session and supervision feedback. During the first phase, the team brainstormed concepts around escape rooms and “more than human” themes, narrowing from a broad pool to three candidates, one of which was a virus spread concept. The Virus was selected for its strong thematic hook and for its clear shared physical setup. Bodystorming sessions, guided by one of our supervisors, were used throughout to test which interactions felt natural in shared physical space. 

The second phase was to transform this concept into an experience where the initial virus game idea was more towards a competitive concept. While it had strong social dynamics, the team challenged collaborative incentives (“Why would I help another player?”). This resulted in another bodystorming session, which pushed the concept toward a more collaborative direction.

In the third phase of the concept evaluation, the team shifted to a cooperative experience with role-based skills. After further supervision feedback emphasising simplicity, the concept was further streamlined into its final form: a replication game where players collaboratively modify virus objects to match a reference formation.

### Design goals

The team set out to achieve the following:
- Create a collaborative MR experience where no single player can succeed alone, requiring genuine communication and teamwork.
- Make the experience accessible and intuitive for a younger audience with some experience of VR/MR, with minimal onboarding and clear visual feedback.
- Ground the game in real science concepts without making it feel like a lesson.
- Built for Meta Quest, using hand tracking as the primary input and avoiding controllers entirely.
- Use the physical table as a shared anchor point to help MR feel grounded rather than abstract.
- Keep the core mechanic simple and polished rather than spreading effort across many half-finished features.

### Design decisions and justifications
- Hand tracking over controllers: The decision to use hand tracking exclusively was driven by the target audience and the nature of the interactions. Also, it supports the lab metaphor, where scientists work very hands-on.
- Puzzle mechanic (replication challenge): Each round presents a locked example formation: a cluster of viruses with specific properties displayed in a physical box that the player has to open to reveal the sample formation. Players receive unmodified viruses and a placeholder formation showing the correct structure with empty slots. They must use their individual powers to modify each virus to match the example, then place it into the correct slot. The slot uses the same snapping logic as the petri dish and validates whether the properties are correct. This mechanic was chosen over simpler alternatives because it requires players to study the formation together, discuss who needs to do what, and coordinate their actions, which creates genuine collaborative problem-solving rather than parallel individual tasks.
- Snapping mechanic (petri dish): The team wanted the viruses to feel tangible and alive, but testing revealed that performing interactions like swiping or scaling was difficult while also holding the virus. To solve this, each player's workstation includes a 3D-modelled petri dish with a snap function. Players place the virus in the dish to lock it in position, freeing both hands to perform their skill interaction without needing to hold the object at the same time.
- Player toolkit design: Each player's workstation is a steel tray divided into three zones: a header strip (player name/role), a left column (skill tutorial video and description), and a main workspace with the virus holder. Skill information is placed on each player's own tray rather than a shared panel, so players only see what is relevant to their role and can focus without distraction. Video tutorials were chosen over written guidelines because the target audience can grasp a visual demonstration much faster than text, especially while wearing a headset. The tray was modelled in Blender with real-world measurements matching the physical table and exported as FBX with transforms applied to preserve correct scaling in Unity.

### Challenges encountered
- Concept uncertainty. The team went through multiple concept iterations before landing on the final replication game. Early ideas (Ghost, Tabletop) were discarded due to scope concerns, and the first fully developed concept (Viral Cascade, competitive approach) was later pivoted away from after supervision feedback highlighted weak collaborative incentives. Each pivot preserved some technical work but required rethinking the design and concept multiple times.
- Networking boundaries. The team split work across branches, with one member handling Photon Fusion 2 networking and others handling local interactions. Keeping interaction scripts free of networking dependencies (no NetworkBehaviour, no [Networked] properties, no Fusion RPCs) was essential to avoid merge conflicts and maintain clean separation of concerns.

## Features and Functionalities

### Core gameplay
- **Collaborative virus replication:** 3 players study a reference virus formation and work together to recreate it using their unique abilities.
- **Role-based skills:** Each player has one exclusive interaction for modifying the virus (e.g. colour, scale, pulsation). No player can complete the puzzle alone.
- **Snap-to-workspace mechanic:** The virus snaps into a player's personal petri dish workstation, locking it in place while they apply their modification.

<img width="5186" height="2592" alt="image" src="https://github.com/user-attachments/assets/e08af6de-1147-4013-a011-9d7a9422e98f" />


### Interactions

Interaction accessible for all players:
- **Scale (two-hand pinch):** Use both hands to scale the virus up or down. Uses Meta SDK's grab transformer.
- **Grab and pass:** Pick up the virus with hand tracking and pass it to other players or place it in a workstation.
- **Room status indicator (LED strips):** The LED strips signal the state of the lab. When players enter the room, the lights are red, indicating the area is contaminated. Once all viruses are successfully matched (the win state), the lights turn green, signalling that the area has been decontaminated. This ties into the overall narrative: players are tasked with replicating the viruses in order to decontaminate the lab. The strips used were SJ-10030-2811 by Shiji Lightning, programmed through the Arduino IDE.

Skill-specific interactions:
- **Colour change (hand swipe):** Swipe your hand over the virus to cycle through different colour (materials) variants. Swipe right for next, swipe left for previous.
- **Virus formation/structure change:** Swipe your hand over the virus to change the virus formations (mesh) variants. Swipe right for next, swipe left for previous.
- **Pulsation activation (tangible):** Blow or clap into a physical microphone sensor to activate the virus pulsation. Press an arcade button to deactivate it. The housing for the hardware was built using LEGO. Hardware components included:
  - Sound sensor microphone amp module (MAX9814)
  - 60mm arcade push button
  - SparkFun ESP32-S2
  - ESP Zero
  - Arduino IDE

### Environment
The experience follows a clinical lab aesthetic with sterile, neutral surfaces contrasted by vibrant virus objects. A visual system was developed to ensure consistency across both physical elements (lab coats, gloves, printed documents, posters) and virtual elements (toolkit trays, petri dishes, virus objects). The colour palette uses dark teal tones and steel neutrals for surfaces and UI, with bright colours reserved for viruses and feedback states (red/green for incorrect/correct). The typeface Orbitron was chosen for its sci-fi lab tone. The following moodboard, sourced mainly from Pinterest, guided the visual direction:

<img width="1043" height="716" alt="image" src="https://github.com/user-attachments/assets/4a8eb6cf-0d7d-4e0e-b680-f1cc1a006fb4" />

#### Mixed reality lab: 
The physical environment was set up as an isolated lab area using white wall dividers, with props including lab coats, a large instruction poster, and printed mission briefing documents. A large white table in the centre served as the shared play surface. Virtual elements such as virus objects, player workstations and UI panels were overlaid on the physical table through Quest passthrough, blending the real and virtual spaces into a single coherent lab experience.

<img width="6056" height="4424" alt="image" src="https://github.com/user-attachments/assets/7de1624e-5702-4672-8fd5-aa34c1b9427b" />

#### Player workstations:
Each player has an individual steel tray with UI panels showing their role name, a skill tutorial video and a skill description. The tray was designed in Blender as a virtual MR object, and a physical replica was also printed to reinforce the mixed reality experience.

<img width="5224" height="3368" alt="image" src="https://github.com/user-attachments/assets/bf016aa8-c157-4f60-b9c5-60779e13d050" />

<img width="1234" height="686" alt="image" src="https://github.com/user-attachments/assets/45e33abe-7677-4e03-8da6-d58d8f6f7c77" />

<img width="7300" height="1684" alt="image" src="https://github.com/user-attachments/assets/2696e726-a8a1-48f9-9971-052e99a27b99" />

#### Instructions canvas:
A physical step-by-step reminder placed above the table, outlining the game loop and how to reach the goal. This was kept physical rather than virtual so that players can read it whether they are wearing a headset or not.

<img width="2380" height="336" alt="image" src="https://github.com/user-attachments/assets/0b5fa3a4-4301-4d06-8290-e1115c9b7408" />

#### The example formation:
The target virus formation was placed inside a physical box on the table, with occlusion hiding the virtual viruses while the lid was closed. Players had to physically open the box to reveal the virtual content inside. This was a key mixed reality design choice: tying the reveal to a tangible action rather than a UI button or timer, using the physical environment to gate access to virtual information.

<img width="1044" height="640" alt="image" src="https://github.com/user-attachments/assets/6a9d685f-bd6c-4281-99b3-38e7148d6f71" />


### Technical
- **Networked multiplayer:** Photon Fusion 2 with co-location support for shared physical space.
- **Spatial Anchors:**  A QR code placed at the centre of the physical table served as the spatial anchor point. All virtual objects (workstations, virus holders, placeholder formation) were spawned relative to this position, ensuring consistent alignment between the physical table and the virtual elements across all headsets.
- **Custom shader:** The virus uses a single Shader Graph with an RGB channel-packed mask texture: red masks the body, green masks the spikes (also for vertex displacement for the pulse effect), blue masks the vein/glow overlay. Three colour properties drive each zone independently at runtime via MaterialPropertyBlock, preserving GPU instancing across all virus instances.: only the index travels over the network, not the colour data itself
- **PC Spectator build:**  A non-VR desktop build runs as an additional client in the same Photon session, used during the demo and testing to observe the shared play space from an external viewpoint without occupying one of the three player roles.

## Installation

The following section explains how to build and run The Virus Lab.

### Prerequisites

- Unity 6.3 LTS (6000.3.10f1) with Android Build Support module
- Meta XR SDK / Meta Interaction SDK
- Photon Fusion 2 SDK
- Git
- Meta Quest 3 headset with Developer Mode enabled

### Setup

1. Clone the repository:
```bash
   git clone https://github.com/HaseemUlHaq/Virus-Hot-Potato.git
```
2. Open the project in Unity Hub. The first import may take several minutes.
3. If prompted to import TMP Essential Resources, accept the import.
4. All required packages (Meta XR SDK, Photon Fusion 2, TextMeshPro, URP) should be included in the project. If any are missing, install them via Window > Package Manager.

### Build and deploy

1. In File > Build Settings, select Android as the platform.
2. Under Player Settings > XR Plug-in Management, ensure Meta XR is enabled for Android.
3. Connect the Quest headset via USB-C and go to File > Build and Run.

### Co-location setup

1. On each Quest headset, go to Settings > Physical Space > Room Scan.
2. Scan the room where the physical table is located.
3. Launch the game on all headsets. The networking layer handles session discovery and spatial alignment.

## Usage

This section explains how to use the experience and interact with its features.

### Starting a session

1. Put on the Quest headset. The game launches in mixed reality mode with the physical room visible through passthrough.
2. Position a table (approximately 180 × 80 × 130 cm) in the play area. The experience is built around a table-top view, so the table size should match closely for the virtual elements to align correctly.
3. Each player stands at one side of the table, where their personal workstation and toolkit are positioned.

### Gameplay flow

1. **Reveal.** Open the physical box to reveal the target virus formation you need to replicate.
2. **Collect.** Pick up an unmodified virus from the table and place it in your personal toolkit.
3. **Modify and pass.** Apply your expertise to the virus and pass it on to the next specialist.
4. **Place.** Place the finished virus in the round holder in the centre of the table.
5. **Complete.** When all four viruses are matched, decontaminate the area.

### Controls

All interactions use hand tracking (no controllers):

| Action | Gesture |
|---|---|
| Grab virus | Reach out and close your hand around it |
| Release virus | Open hand while holding |
| Scale virus | Grab with both hands and move apart/together |
| Place in workstation | Move virus near the petri dish; it snaps automatically |
| Change colour (skill) | Swipe open hand left or right across docked virus |
| Change formation (skill) | Swipe open hand left or right across docked virus |
| Activate pulsation (skill) | Blow or clap near the virus |
| Deactivate pulsation (skill) | Press the physical button |

### Tips

- Study the target formation from multiple angles before starting. You can walk around the table to see it from different sides.
- Communicate with your teammates about which properties still need to be matched.
- If the virus is not snapping into the workstation, make sure you are releasing it close enough to the petri dish centre.
  
## References
 
- **Meta XR SDK / Meta Interaction SDK** — Hand tracking, grab interactions, and the snapping/interaction patterns were built on Meta's SDKs. The spray bottle interaction from Meta's example scenes was used as a reference for grab-and-use interactions.
- **Meta sample materials** — Some materials from Meta's sample assets were used or adapted for the environment and objects.
- **Photon Fusion 2** — Networking and co-location support.
- **Orbitron** — Typeface used across the UI, by Matt McInerney (SIL Open Font License).
- **Moodboard imagery** — Visual references sourced mainly from Pinterest, used for design direction only.
- **Arduino IDE** — Used along with associated libraries for the hardware components (MAX9814 sound sensor, ESP32-S2, LED strips).
## License
 
This project was developed as part of the DCDC VT26 course at Stockholm University (DSV) and is shared for educational and portfolio purposes. Third-party assets (Meta XR SDK samples, Photon Fusion 2, Orbitron) remain under their respective licenses.

## Contributors

| Name | Email |
|---|---|


| Tindra Heurlin | tindra.heurlin@gmail.com |
| Lorena Livadaru | livadaru.design@gmail.com |
| Shashank Salgarkar | salgarkarshashank@gmail.com |
| Haseem Ul Haq | haseemulhaq@gmail.com |
## Video

[▶ Watch the Virus Lab video](https://drive.google.com/drive/u/0/folders/1fBl5Hdnyb7Shkrvboc-8jWBsb-VmhUpD)
