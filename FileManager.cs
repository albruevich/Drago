// FileManager handles loading and saving the high score.
// It hides all file system details and exposes simple Load/Save methods.
// This keeps file operations separate from the game logic (Game class).

using System;                      // basic C# types
using System.IO;                   // working with files

namespace Drago
{
    class FileManager
    {
        string hiScorePath;  // FileManager state is the path to the high score file (encapsulation)

        public FileManager(string appFolderName, string fileName)
        {
            // OOP: FileManager hides "where exactly" the file is saved. It only exposes Load/Save methods.           
            hiScorePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                appFolderName, fileName);
        }

        public float LoadHiScore()
        {
            // Try to read the record safely (the game should not crash because of a file)
            try
            {
                if (File.Exists(hiScorePath) && int.TryParse(File.ReadAllText(hiScorePath), out int value))
                    return value;
            }
            catch { }
            return 0;
        }

        public void SaveHiScore(int hiScore)
        {
            // Save the record safely: create the folder and write the file
            try
            {
                string dir = Path.GetDirectoryName(hiScorePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(hiScorePath, hiScore.ToString());
            }
            catch { }
        }
    }
}
