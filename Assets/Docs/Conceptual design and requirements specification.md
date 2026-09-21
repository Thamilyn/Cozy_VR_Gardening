# Conceptual design and requirements specification

Cozy VR Garden

Thamara Gonzalez & Jonathan Guasco

UIA – Grimstad

31-08-2026


## 1.Scenario of Use

Cozy VR Garden

Cozy VR Garden is a guided virtual reality experience designed to teach users basic principles of plant care, sustainable gardening and biodiversity through hands-on interaction.

The experience is aimed at users with little or no previous experience with VR or gardening. The goal is to make learning accessible and engaging by allowing users to learn through doing rather than mainly through reading or listening to information.

The user enters a small virtual garden where they are guided by a virtual gardening instructor. The instructor provides short audio instructions and explains the needs of different plants and the importance of creating a healthy and sustainable garden.

The user is introduced to a small selection of plants and gardening tools. They can pick up and move objects such as pots, seeds and a watering can using the VR controllers. The user then completes simple gardening tasks, such as planting seeds, watering plants and choosing appropriate plants for different areas of the garden.

As the user interacts with the environment, the plants provide visual feedback based on their actions. For example, a plant may change its appearance after receiving the appropriate care. The instructor also provides audio feedback to reinforce important information and guide the user through the experience.

The experience gradually moves from guided activities to independent interaction. At the beginning, the instructor provides clear step-by-step instructions. Towards the end, the user is given a small gardening task where they must apply what they have learned without being told exactly what to do. This allows the application to assess whether the user can apply basic plant-care and sustainable gardening principles.

The experience will focus on a small number of relevant gardening and biodiversity topics. The specific plants and practices will be selected through further research and, if possible, input from gardening experts or organisations such as Royal Gardens.

VR is particularly suitable for this scenario because it allows users to learn through spatial and physical interaction. Instead of simply selecting options on a screen, users can physically pick up a watering can, move a plant pot and interact with the garden using their VR controllers. This supports an embodied learning experience where users can learn by performing the actions themselves.

User comfort will also be considered throughout the experience. Smooth locomotion using the VR controller will allow users to walk naturally around the garden. The environment will be kept relatively small, and movement will be kept at a comfortable


speed to reduce the risk of cybersickness. Interactive objects will be placed within comfortable reach whenever possible.

## 2. Storyboard

*Illustration 1 Cozy Garden Storyboard, the user will be guided though the steps to grew different plants and should apply the knowledge acquired in a final task*

## 3. Conceptual Model

The conceptual model of Cozy VR Garden is based on a guided learning relationship between the user, virtual instructor, plants, gardening objects and garden environment.

The user is the main actor. The user explores the garden, listens to the instructor, interacts with gardening objects and takes care of the plants.

The virtual instructor acts as a guide throughout the experience. It provides audio instructions, introduces information about the plants, explains gardening tasks and gives feedback to the user.


The plants are the main learning objects. Each plant has specific needs, such as water, light and time to grow. Plants can change their visual state in response to the user's actions and the passage of time.

The gardening objects allow the user to perform actions in the environment. These can include seeds, pots, soil, a watering can and gardening tools. The user can pick up, move and use these objects through VR interactions.

The garden environment provides the context for the experience. It contains the plants, gardening objects and different areas for learning and interaction.

A calendar/time system allows the user to advance the virtual time and observe plant growth. This allows the user to experience a growing process that would normally take weeks or months within a short VR session.

The experience follows the relationship:

Instructor → teaches → User → interacts with → Gardening Objects → affects → Plants → provide feedback → User

The user gradually moves from guided learning to independent application, where they must use the knowledge acquired during the experience to decide how to care for their plant.

## Conceptual Model Diagram

Illustration 2 The conceptual model represents the main elements of the experience and their relationships. The user is guided by a virtual instructor and interacts with gardening objects to care for plants. The calendar allows virtual time to progress, while plant changes provide feedback. Through these interactions, the user learns basic plant-care practices and finally applies the acquired knowledge in an independent task.


## 4. Functional Requirements Specifications

## Navigation and Movement

- FR-01: Movement

The system shall allow the user to move around the virtual garden using the VR controller.

- FR-02: Turning

The system shall allow the user to rotate their view using the VR controller.

- FR-03: Movement Comfort

The system shall provide a comfortable movement speed suitable for a beginner VR user.

## Object Interaction

- FR-04: Object Manipulation

The system shall allow the user to pick up and move interactive gardening objects using the VR controllers.

- FR-05: Object Placement

The system shall allow the user to place gardening objects in designated areas.

- FR-06: Tool Interaction

The system shall allow the user to use gardening tools through controller interactions.

- FR-07: Interactive Objects

The system shall provide at least five different interactive objects with distinguishable functions.

## Planting and Plant Care

- FR-08: Planting

The system shall allow the user to place seeds in a plant pot.

- FR-09: Watering

The system shall allow the user to water the plant using a watering can.

- FR-10: Plant Requirements

The system shall communicate the basic care requirements of each selected plant.

- FR-11: Plant Response

The system shall change the visual state of a plant in response to relevant care actions.


## Time and Growth

## FR-12: Time Progression

The system shall allow the user to advance virtual time using an interactive calendar.

## FR-13: Plant Growth

The system shall represent changes in plant growth as virtual time progresses.

## FR-14: Growth Feedback

The system shall provide visual feedback when a significant growth stage is reached.

## Educational Guidance

## FR-15: Audio Guidance

The system shall provide audio instructions through a virtual gardening instructor.

## FR-16: Task Instructions

The system shall provide instructions for each main gardening activity.

## FR-17: Feedback

The system shall provide audio and/or visual feedback after relevant user actions.

## FR-18: Progressive Guidance

The system shall reduce direct instructions as the user progresses through the experience.

## Learning Assessment

## FR-19: Independent Task

The system shall provide a final gardening task in which the user must apply knowledge acquired during the guided activities.

- FR-20: Correct Actions

The system shall detect whether the user performs the required actions correctly.

## FR-21: Error Feedback

The system shall provide feedback when the user performs an incorrect action during the final task.

## User Interface

## FR-22: VR Menu

The system shall provide a functional VR menu.

## FR-23: Settings

The system shall allow the user to adjust basic settings, including audio volume.


## FR-24: Instructions

The system shall provide accessible instructions for the main interactions.

## 5. Reflection Note on Design Considerations

The design of Cozy VR Garden focuses on creating a simple and accessible VR learning experience where users can learn basic sustainable gardening practices through interaction. Rather than designing a complex gardening simulator, the project focuses on a small number of meaningful interactions that allow users to learn by doing. This decision is important because the target users may have little or no previous experience with VR or gardening.

## User Experience and Interaction

One of the main design considerations is to make the interactions feel natural and easy to understand. Gardening is an activity that already involves physical actions such as picking up objects, planting seeds and watering plants. VR allows these actions to be represented directly through the user's hands and controllers.

The main interactive objects will include seeds, a plant pot, soil, a watering can and other simple gardening tools. These objects will be designed to communicate their purpose through their appearance and position in the environment. For example, a watering can should look and behave like an object that can be picked up and used for watering.

The experience will avoid unnecessary complex interactions because the user should be able to focus on learning rather than learning how to use the interface. Feedback will be provided when important actions are completed so that users understand whether their interaction was successful.

## Guided Learning

The educational experience will be guided by a virtual gardening instructor. Instead of presenting large amounts of written information, the instructor will provide short audio instructions while the user performs the gardening activities.

For example, the instructor may introduce tomatoes and explain their basic requirements before asking the user to plant the seeds. Later, the instructor can explain the importance of appropriate watering and sunlight while the user performs these actions.

This creates a learning cycle of:

## Listen → Practice → Receive feedback → Apply

The amount of guidance will gradually decrease throughout the experience. At the beginning, the instructor will provide clear step-by-step instructions. Towards the end,


the user will be given an independent task where they need to apply what they have learned without receiving the exact solution.

This is important because the goal is not simply for users to complete instructions, but to determine whether they can understand and apply basic gardening knowledge.

## Learning and Feedback

Feedback is an important part of the design because users need to understand the relationship between their actions and the plant's condition. Plants will therefore have different visual states that represent their progress.

For example, after planting and providing the appropriate care, the tomato plant can gradually change from a seed to a young plant and eventually to a mature plant. This provides a visual representation of the consequences of the user's actions.

The virtual calendar will also allow the user to advance time. Since real plant growth can take weeks or months, allowing time to progress within the experience makes it possible to demonstrate the relationship between care, time and plant growth without requiring the user to wait in real time.

The final activity will work as a simple learning assessment. Instead of using a traditional quiz, the user will be asked to identify what a plant needs and perform the appropriate action. This keeps the assessment consistent with the hands-on nature of the experience.

## Sustainable Gardening and Biodiversity

The educational content will focus not only on growing plants but also on introducing basic principles of sustainable gardening and biodiversity. This direction is inspired by the work and values presented by Royal Gardens, including sustainable gardening, biodiversity and environmental awareness.

The exact plants and gardening practices used in the experience will be selected through further research. Where possible, expert feedback will also be considered to ensure that the information presented in the experience is relevant and accurate.

The garden itself can communicate biodiversity visually. For example, plants that support pollinators could be included in the environment, allowing users to see that a garden can provide benefits beyond the individual plants being cultivated.

This approach also gives the experience a broader educational purpose: users learn that caring for a garden involves understanding the needs of plants while also considering the surrounding environment.


## Navigation and Cybersickness

User comfort is another important consideration when designing the VR experience. The user will move around the garden using smooth locomotion through the VR controller, allowing them to explore the environment naturally.

However, continuous artificial movement can contribute to cybersickness for some users. To reduce this risk, the garden will remain relatively small, movement speed will be kept comfortable, and unnecessary camera movement will be avoided. Snap turning can also be considered instead of continuous rotation.

Most gardening activities will take place while the user is standing relatively still. This reduces the amount of movement required while performing detailed interactions such as planting seeds or using the watering can.

These decisions will also be evaluated during user testing, since comfort and ease of navigation can vary considerably between users.

## Accessibility and Beginner Users

Because the application is intended for beginners, accessibility and simplicity are important design considerations. Instructions will be short and primarily delivered through audio, supported by visual cues when necessary.

The user should not need to remember complicated controller combinations. Interactions will rely mainly on familiar actions such as grabbing, placing and using objects.

The physical arrangement of the garden will also consider the user's reach. Important objects should be positioned at comfortable distances so that users do not need to repeatedly bend, stretch or reach excessively.

## Visual and Audio Design

The visual style will use a cozy, cute and low-poly aesthetic. This style supports the relaxing character of the garden while keeping the environment relatively simple to develop.

The visual design will also help distinguish interactive objects from decorative elements. Important objects should be recognizable and visually understandable without requiring additional text.

Audio is particularly important because the virtual instructor is responsible for delivering much of the educational content. Instructions should therefore be short, clear and appropriately timed. The instructor should not speak while the user is performing an important action, as this could make the information difficult to process.


## User Testing

User testing will be used to evaluate whether the design supports the intended learning experience. The first testing round will focus primarily on usability and interaction. We will observe whether users understand how to move, pick up objects, plant seeds and use the watering can without excessive assistance.

The second testing round will focus on improvements and learning outcomes. We will investigate whether users can independently apply the information introduced during the guided activities.

Possible measures include task completion, errors, time taken to complete tasks and the user's ability to explain or demonstrate what the plant needs. User feedback will also help identify problems with navigation, audio instructions, object placement and general comfort.

Overall, the design of Cozy VR Garden aims to balance educational value, intuitive interaction and technical simplicity. By limiting the scope to a small garden and a few meaningful gardening activities, the project can focus on demonstrating effective VR interaction and learning rather than complex simulation. The final experience should allow users to understand basic plant-care and sustainable gardening concepts by actively participating in the virtual garden.


## References

International Organization for Standardization. (2019). ISO 9241-210:2019: Ergonomics of human-system interaction — Part 210: Human-centred design for interactive systems. ISO. https://www.iso.org/standard/77520.html [URL 🔗](https://www.iso.org/standard/77520.html)

International Organization for Standardization. (2018). ISO/IEC/IEEE 29148:2018: Systems and software engineering — Life cycle processes — Requirements engineering. ISO. https://www.iso.org/standard/72089.html [URL 🔗](https://www.iso.org/standard/72089.html)

Royal Gardens. (n.d.). About us. https://royalgardens.no/about-us/ [URL 🔗](https://royalgardens.no/about-us)

Royal Gardens. (n.d.). Research. https://royalgardens.no/research/ [URL 🔗](https://royalgardens.no/research/)

Unity Technologies. (n.d.). XR Interaction Toolkit: Locomotion. Unity Documentation. XR-Interaction-Toolkit-Examples/Documentation/LocomotionSetup.md at main · Unity- Technologies/XR-Interaction-Toolkit-Examples · GitHub [URL 🔗](https://github.com/Unity-Technologies/XR-Interaction-Toolkit-Examples/blob/main/Documentation/LocomotionSetup.md?utm_source=chatgpt.com)

Unity Technologies. (n.d.). XR Interaction Toolkit Examples. GitHub.

[https://github.com/Unity-Technologies/XR-Interaction-Toolkit-Examples](https://github.com/Unity-Technologies/XR-Interaction-Toolkit-Examples)
