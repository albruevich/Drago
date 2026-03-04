/*

BONUS

OOP principles (Object-Oriented Programming)

The following section briefly explains the OOP concepts used in this project.

1. Encapsulation – combining data and the methods that operate on that data inside a class,
   while controlling access to the internal state.

   Example: in the Dino class there is the field 'y' and the methods Jump() and Update().
   External code cannot change 'y' directly, which prevents the jump logic from being broken.

   Why: protects internal data and simplifies maintenance.

2. Inheritance – when one class derives from another and reuses its behavior.

   Example: Obstacle is a base class, while Bush and Bird are derived classes.
   They automatically receive fields like 'x' and methods like Update(), and can add their own behavior.

   Why: enables code reuse and object hierarchies.

3. Polymorphism – the ability to treat different objects through the same interface.

   Example: both Bush and Bird implement Draw(). The game calls Draw() without
   caring which specific obstacle it is.

   Why: the same code can work with different object types.

4. Abstraction – exposing only the essential features while hiding implementation details.

   Example: the abstract class Obstacle defines Width, Height and Draw(),
   but the actual drawing logic is implemented in derived classes.

   Why: simplifies working with complex systems.

5. Information hiding – internal implementation details of a class are not accessible from outside.

   Example: fields like symbols in Bush or y in Dino cannot be modified directly.

   Why: prevents incorrect usage of objects.

6. Composition (or aggregation) – building complex objects from smaller objects.

   Example: Game contains a List<Obstacle>. Each obstacle is an independent object,
   but together they form the game world.

   Why: allows building complex systems from simple components.

7. Message passing / Methods as an interface – objects communicate through method calls.

   Example: dino.Jump() starts a jump, and Update() processes the internal state.

   Why: objects control their own behavior instead of exposing internal data.



| OOP principle                          | Where in the code                        | Concrete example                                |
|----------------------------------------|------------------------------------------|-------------------------------------------------|
| Encapsulation                          | Dino                                     | Field `y` is hidden; `Jump()` controls the jump |
| Information hiding                     | Bush and Obstacle                        | Private fields `symbols`, `x`, `oldX`           |
| Inheritance                            | Bush : Obstacle, Bird : Obstacle         | Derived classes reuse base class logic          |
| Polymorphism                           | List<Obstacle> obstacles                 | The list contains different obstacle types      |
| Abstraction                            | abstract class Obstacle                  | Defines `Width`, `Height`, `Draw()`             |
| Composition / Aggregation              | Game                                     | Contains the list of obstacles                  |
| Message passing / Methods as interface | Calls to dino.Jump(), obstacle.Draw(...) | Objects communicate through methods             |

*/