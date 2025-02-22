using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TaskScheduler.Models;

namespace TaskScheduler.Utility
{
    public static class TaskDataManager
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VPetToDoListMod",
            "tasks.json"
        );

        public static void SaveTasks(List<TaskItem> tasks)
        {
            // Ensure directory exists. Use a safe check.
            string dir = Path.GetDirectoryName(FilePath);
            if (dir != null)
            {
                Directory.CreateDirectory(dir);
            }

            // Serialize to JSON
            string json = JsonSerializer.Serialize(tasks, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            // Write to file
            File.WriteAllText(FilePath, json);
        }

        public static List<TaskItem> LoadTasks()
        {
            if (!File.Exists(FilePath))
                return new List<TaskItem>();

            string json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<TaskItem>>(json) ?? new List<TaskItem>();
        }
    }
}
