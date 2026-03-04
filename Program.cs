//================================================
//                     Drago
//================================================


// This small console game demonstrates the core principles of OOP (Object-Oriented Programming).
// The project is intentionally kept simple and heavily commented to help beginners understand
// how a real program is structured and how objects interact with each other.
//
// The code shows practical examples of encapsulation, inheritance, polymorphism,
// and basic game architecture in C#.
//
// A short explanation of the OOP principles used in this project can be found
// in the separate file: Bonus_OOP.cs.
//
// If this project helped you understand OOP, consider giving it a star ⭐


namespace Drago
{
    // Program entry point — execution starts here.
    class Program
    {
        static void Main() => new Game().Run();
    }

    // Screen mode: day or night    
    enum DayTime
    {
        Day,
        Night
    }
}