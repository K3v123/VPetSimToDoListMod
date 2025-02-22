#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TaskScheduler.Models;   // For TaskItem
using TaskScheduler.Utility;  // For TaskDataManager

namespace TaskScheduler
{
    // Data class for drag-and-drop.
    public class TaskData
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public string Text { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public partial class MainWindow : Window
    {
        private Point _dragStartPoint;

        // Global dictionary: each TaskID maps to a list of all clones.
        private static readonly Dictionary<Guid, List<Border>> globalTaskInstances = new Dictionary<Guid, List<Border>>();

        // Shared marker state for each task.
        // Each List<bool> represents the markers for that task (true = ticked "●", false = unticked "○").
        private static readonly Dictionary<Guid, List<bool>> taskMarkerStates = new Dictionary<Guid, List<bool>>();

        // Local list for saving/loading.
        private List<TaskItem> _savedTasks = new List<TaskItem>();

        public MainWindow()
        {
            InitializeComponent();

            // Load tasks on startup.
            _savedTasks = TaskDataManager.LoadTasks();
            RebuildUIFromSavedTasks();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            SaveAllTasksToDisk();
        }

        // Rebuild the UI from _savedTasks.
        private void RebuildUIFromSavedTasks()
        {
            // Clear main category panels and task pool.
            MostImportantUrgentStack.Children.Clear();
            ImportantNotUrgentStack.Children.Clear();
            UrgentNotImportantStack.Children.Clear();
            NotImportantNotUrgentStack.Children.Clear();
            TaskPoolList.Items.Clear();

            // Clear global dictionaries.
            globalTaskInstances.Clear();
            taskMarkerStates.Clear();

            foreach (var item in _savedTasks)
            {
                // Ensure marker state exists.
                if (!taskMarkerStates.ContainsKey(item.ID))
                    taskMarkerStates[item.ID] = new List<bool>(item.MarkerStates);

                // Create a card.
                Border card = CreateTaskCard(new TaskData
                {
                    ID = item.ID,
                    Text = item.Text,
                    Category = item.OriginalCategory
                });
                card.PreviewMouseLeftButtonDown += TaskCard_PreviewMouseLeftButtonDown;
                card.PreviewMouseMove += TaskCard_MouseMove;

                // If a DayAssignment exists, add to the appropriate day panel.
                if (!string.IsNullOrEmpty(item.DayAssignment))
                {
                    // Mark this clone as coming from a day panel.
                    if (card.Tag is TaskCardTagData tagData)
                        tagData.IsDayPanel = true;
                    switch (item.DayAssignment)
                    {
                        case "Monday":
                            AddTaskToDayPanel(card, MondayPanel);
                            break;
                        case "Tuesday":
                            AddTaskToDayPanel(card, TuesdayPanel);
                            break;
                        case "Wednesday":
                            AddTaskToDayPanel(card, WednesdayPanel);
                            break;
                        case "Thursday":
                            AddTaskToDayPanel(card, ThursdayPanel);
                            break;
                        case "Friday":
                            AddTaskToDayPanel(card, FridayPanel);
                            break;
                        case "Saturday":
                            AddTaskToDayPanel(card, SaturdayPanel);
                            break;
                        case "Sunday":
                            AddTaskToDayPanel(card, SundayPanel);
                            break;
                        default:
                            // If day not recognized, add to task pool.
                            TaskPoolList.Items.Add(card);
                            break;
                    }
                }
                else
                {
                    // No day assignment: add to main category.
                    switch (item.OriginalCategory)
                    {
                        case "MostImportantUrgent":
                            MostImportantUrgentStack.Children.Add(card);
                            break;
                        case "ImportantNotUrgent":
                            ImportantNotUrgentStack.Children.Add(card);
                            break;
                        case "UrgentNotImportant":
                            UrgentNotImportantStack.Children.Add(card);
                            break;
                        case "NotImportantNotUrgent":
                            NotImportantNotUrgentStack.Children.Add(card);
                            break;
                        default:
                            TaskPoolList.Items.Add(card);
                            break;
                    }

                    // Also create a clone for the TaskPool.
                    Border poolCard = CreateTaskCard(new TaskData
                    {
                        ID = item.ID,
                        Text = item.Text,
                        Category = item.OriginalCategory
                    });
                    poolCard.PreviewMouseLeftButtonDown += TaskCard_PreviewMouseLeftButtonDown;
                    poolCard.PreviewMouseMove += TaskCard_MouseMove;
                    TaskPoolList.Items.Add(poolCard);
                }
            }
        }

        // Save tasks to disk by processing both main stacks and day panels.
        private void SaveAllTasksToDisk()
        {
            _savedTasks.Clear();

            // Process main category panels.
            void ProcessStackChildren(StackPanel sp, string category)
            {
                foreach (var child in sp.Children)
                {
                    if (child is Border card && card.Tag is TaskCardTagData tagData)
                    {
                        Guid id = tagData.TaskID;
                        string text = tagData.TaskTextBlock.Text;
                        List<bool> states = taskMarkerStates.ContainsKey(id)
                            ? taskMarkerStates[id]
                            : new List<bool>();
                        _savedTasks.Add(new TaskItem
                        {
                            ID = id,
                            Text = text,
                            OriginalCategory = category,
                            DayAssignment = string.Empty,
                            MarkerStates = new List<bool>(states)
                        });
                    }
                }
            }

            ProcessStackChildren(MostImportantUrgentStack, "MostImportantUrgent");
            ProcessStackChildren(ImportantNotUrgentStack, "ImportantNotUrgent");
            ProcessStackChildren(UrgentNotImportantStack, "UrgentNotImportant");
            ProcessStackChildren(NotImportantNotUrgentStack, "NotImportantNotUrgent");

            // Process day panels.
            void ProcessDayPanel(Border dayPanel, string dayName)
            {
                if (dayPanel.Child is Panel dayStack)
                {
                    foreach (var child in dayStack.Children)
                    {
                        if (child is Border card && card.Tag is TaskCardTagData tagData)
                        {
                            Guid id = tagData.TaskID;
                            string text = tagData.TaskTextBlock.Text;
                            List<bool> states = taskMarkerStates.ContainsKey(id)
                                ? taskMarkerStates[id]
                                : new List<bool>();
                            _savedTasks.Add(new TaskItem
                            {
                                ID = id,
                                Text = text,
                                OriginalCategory = tagData.Category,
                                DayAssignment = dayName,
                                MarkerStates = new List<bool>(states)
                            });
                        }
                    }
                }
            }

            ProcessDayPanel(MondayPanel, "Monday");
            ProcessDayPanel(TuesdayPanel, "Tuesday");
            ProcessDayPanel(WednesdayPanel, "Wednesday");
            ProcessDayPanel(ThursdayPanel, "Thursday");
            ProcessDayPanel(FridayPanel, "Friday");
            ProcessDayPanel(SaturdayPanel, "Saturday");
            ProcessDayPanel(SundayPanel, "Sunday");

            TaskDataManager.SaveTasks(_savedTasks);
        }

        // Helper: Add a task card to a day panel.
        private void AddTaskToDayPanel(Border card, Border dayPanel)
        {
            if (dayPanel.Child is Panel dayStack)
            {
                if (card.Tag is TaskCardTagData tct)
                    tct.IsDayPanel = true;
                card.PreviewMouseLeftButtonDown += TaskCard_PreviewMouseLeftButtonDown;
                card.PreviewMouseMove += TaskCard_MouseMove;
                dayStack.Children.Add(card);
            }
        }

        // Helper: Create a circular ControlTemplate for buttons.
        private ControlTemplate CreateCircularButtonTemplate(double diameter)
        {
            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(diameter / 2));
            var cpFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            cpFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cpFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(cpFactory);
            template.VisualTree = borderFactory;
            return template;
        }

        // Helper: Remove a clone from its parent container.
        private void RemoveCloneFromParent(Border clone)
        {
            if (clone.Parent is Panel panel)
                panel.Children.Remove(clone);
            else if (clone.Parent is ItemsControl ic)
                ic.Items.Remove(clone);
        }

        // Placeholder handling.
        private void RemovePlaceholder(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.Text == "Type your task here...")
            {
                tb.Text = "";
                tb.Foreground = Brushes.Black;
            }
        }

        private void RestorePlaceholder(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && string.IsNullOrWhiteSpace(tb.Text))
            {
                tb.Text = "Type your task here...";
                tb.Foreground = Brushes.Gray;
            }
        }

        // Add task button click (Main Tab).
        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button establishButton))
                return;
            string category = establishButton.Tag?.ToString() ?? string.Empty;
            TextBox inputBox = null;
            StackPanel targetStack = null;
            switch (category)
            {
                case "MostImportantUrgent":
                    inputBox = MostImportantUrgentInput;
                    targetStack = MostImportantUrgentStack;
                    break;
                case "ImportantNotUrgent":
                    inputBox = ImportantNotUrgentInput;
                    targetStack = ImportantNotUrgentStack;
                    break;
                case "UrgentNotImportant":
                    inputBox = UrgentNotImportantInput;
                    targetStack = UrgentNotImportantStack;
                    break;
                case "NotImportantNotUrgent":
                    inputBox = NotImportantNotUrgentInput;
                    targetStack = NotImportantNotUrgentStack;
                    break;
            }
            if (inputBox == null || targetStack == null)
                return;
            string text = inputBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(text) || text == "Type your task here...")
                return;

            TaskData task = new TaskData { Text = text, Category = category };

            if (!taskMarkerStates.ContainsKey(task.ID))
                taskMarkerStates[task.ID] = new List<bool> { false };

            Border taskCard = CreateTaskCard(task);
            taskCard.PreviewMouseLeftButtonDown += TaskCard_PreviewMouseLeftButtonDown;
            taskCard.PreviewMouseMove += TaskCard_MouseMove;
            targetStack.Children.Add(taskCard);

            Border poolCard = CreateTaskCard(task);
            poolCard.PreviewMouseLeftButtonDown += TaskCard_PreviewMouseLeftButtonDown;
            poolCard.PreviewMouseMove += TaskCard_MouseMove;
            TaskPoolList.Items.Add(poolCard);

            inputBox.Text = "";

            var markerCopy = new List<bool>(taskMarkerStates[task.ID]);
            _savedTasks.Add(new TaskItem
            {
                ID = task.ID,
                Text = task.Text,
                OriginalCategory = task.Category,
                MarkerStates = markerCopy
            });
            TaskDataManager.SaveTasks(_savedTasks);
        }

        // Create a task card from a TaskData object.
        private Border CreateTaskCard(TaskData task)
        {
            if (!taskMarkerStates.ContainsKey(task.ID))
                taskMarkerStates[task.ID] = new List<bool> { false };

            Border card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 10),
                BorderBrush = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)),
                BorderThickness = new Thickness(1)
            };

            StackPanel mainPanel = new StackPanel();

            Grid headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            TextBlock taskText = new TextBlock
            {
                Text = task.Text,
                MaxWidth = 300,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(taskText, 0);
            Button deleteButton = new Button
            {
                Content = "✕",
                Width = 30,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Foreground = Brushes.Gray
            };
            deleteButton.Click += DeleteButton_Click;
            Grid.SetColumn(deleteButton, 1);
            headerGrid.Children.Add(taskText);
            headerGrid.Children.Add(deleteButton);
            mainPanel.Children.Add(headerGrid);

            StackPanel progressPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 0)
            };
            ScrollViewer progressScroll = new ScrollViewer
            {
                Content = progressPanel,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Height = 40
            };
            mainPanel.Children.Add(progressScroll);

            card.Child = mainPanel;

            switch (task.Category)
            {
                case "MostImportantUrgent":
                    card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF5350"));
                    card.BorderThickness = new Thickness(4, 1, 1, 1);
                    break;
                case "ImportantNotUrgent":
                    card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64B5F6"));
                    card.BorderThickness = new Thickness(4, 1, 1, 1);
                    break;
                case "UrgentNotImportant":
                    card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB74D"));
                    card.BorderThickness = new Thickness(4, 1, 1, 1);
                    break;
                case "NotImportantNotUrgent":
                    card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#90A4AE"));
                    card.BorderThickness = new Thickness(4, 1, 1, 1);
                    break;
            }

            var tagData = new TaskCardTagData
            {
                TaskID = task.ID,
                Category = task.Category,
                TaskTextBlock = taskText,
                ProgressPanel = progressPanel,
                IsDayPanel = false
            };
            card.Tag = tagData;

            if (!globalTaskInstances.ContainsKey(task.ID))
                globalTaskInstances[task.ID] = new List<Border>();
            globalTaskInstances[task.ID].Add(card);

            SetupProgressPanel(tagData);

            return card;
        }

        // Tag data class.
        private class TaskCardTagData
        {
            public Guid TaskID { get; set; }
            public string Category { get; set; } = string.Empty;
            public TextBlock TaskTextBlock { get; set; } = new TextBlock();
            public StackPanel ProgressPanel { get; set; } = new StackPanel();
            public bool IsDayPanel { get; set; } = false;
        }

        // Build the progress panel UI.
        private void SetupProgressPanel(TaskCardTagData tagData)
        {
            tagData.ProgressPanel.Children.Clear();
            List<bool> markers = taskMarkerStates[tagData.TaskID];
            for (int i = 0; i < markers.Count; i++)
            {
                Button markerBtn = new Button
                {
                    Content = markers[i] ? "●" : "○",
                    Width = 30,
                    Height = 30,
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Gray,
                    Margin = new Thickness(5, 0, 0, 0),
                    Tag = i
                };
                markerBtn.Click += (s, e) =>
                {
                    int index = (int)((Button)s).Tag;
                    ToggleMarker(tagData.TaskID, index);
                };
                markerBtn.Template = CreateCircularButtonTemplate(30);
                tagData.ProgressPanel.Children.Add(markerBtn);
            }
            if (markers.Count < 3)
            {
                Button plusBtn = new Button
                {
                    Content = "＋",
                    Width = 30,
                    Height = 30,
                    Background = Brushes.LightGray,
                    BorderBrush = Brushes.Transparent,
                    Margin = new Thickness(5, 0, 0, 0)
                };
                plusBtn.Click += (s, e) => { AddMarker(tagData.TaskID); };
                plusBtn.Template = CreateCircularButtonTemplate(30);
                tagData.ProgressPanel.Children.Add(plusBtn);
            }

            // STRIKETHROUGH LOGIC: If all markers are ticked, strike through the text.
            bool allTicked = (markers.Count > 0) && markers.TrueForAll(x => x);
            tagData.TaskTextBlock.TextDecorations = allTicked ? TextDecorations.Strikethrough : null;
        }

        // Toggle the marker and update clones.
        private void ToggleMarker(Guid taskID, int index)
        {
            if (taskMarkerStates.ContainsKey(taskID) && index < taskMarkerStates[taskID].Count)
            {
                taskMarkerStates[taskID][index] = !taskMarkerStates[taskID][index];
                UpdateAllClonesForTask(taskID);
                SaveAllTasksToDisk();
            }
        }

        // Add a marker and update clones.
        private void AddMarker(Guid taskID)
        {
            if (taskMarkerStates.ContainsKey(taskID) && taskMarkerStates[taskID].Count < 3)
            {
                taskMarkerStates[taskID].Add(false);
                UpdateAllClonesForTask(taskID);
                SaveAllTasksToDisk();
            }
        }

        // Update all clones.
        private void UpdateAllClonesForTask(Guid taskID)
        {
            if (globalTaskInstances.ContainsKey(taskID))
            {
                foreach (var clone in globalTaskInstances[taskID].ToArray())
                {
                    if (clone.Tag is TaskCardTagData tagData)
                        SetupProgressPanel(tagData);
                }
            }
        }

        // Delete button click.
        // • If the card is from a day panel (IsDayPanel true), remove only that instance.
        // • Otherwise, remove all clones globally.
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (sender is Button delBtn)
            {
                // Check if the delete button is inside a day panel by looking for known panel names.
                bool isInDayPanel = (FindVisualParentByName(delBtn, "MondayTasks") != null ||
                                     FindVisualParentByName(delBtn, "TuesdayTasks") != null ||
                                     FindVisualParentByName(delBtn, "WednesdayTasks") != null ||
                                     FindVisualParentByName(delBtn, "ThursdayTasks") != null ||
                                     FindVisualParentByName(delBtn, "FridayTasks") != null ||
                                     FindVisualParentByName(delBtn, "SaturdayTasks") != null ||
                                     FindVisualParentByName(delBtn, "SundayTasks") != null);

                Border card = FindVisualParent<Border>(delBtn);
                if (card != null && card.Tag is TaskCardTagData tagData)
                {
                    Guid taskID = tagData.TaskID;
                    if (!isInDayPanel)
                    {
                        // Global deletion: remove all clones.
                        if (globalTaskInstances.ContainsKey(taskID))
                        {
                            foreach (var clone in new List<Border>(globalTaskInstances[taskID]))
                            {
                                RemoveCloneFromParent(clone);
                            }
                            globalTaskInstances.Remove(taskID);
                        }
                        taskMarkerStates.Remove(taskID);
                        _savedTasks.RemoveAll(t => t.ID == taskID);
                        TaskDataManager.SaveTasks(_savedTasks);
                    }
                    else
                    {
                        // Local deletion: remove only this clone.
                        RemoveCloneFromParent(card);
                        if (globalTaskInstances.ContainsKey(taskID))
                            globalTaskInstances[taskID].Remove(card);
                    }
                }
            }
        }

        // Helper: Find a visual parent by Name.
        private FrameworkElement FindVisualParentByName(DependencyObject obj, string name)
        {
            while (obj != null)
            {
                if (obj is FrameworkElement fe && fe.Name == name)
                    return fe;
                obj = VisualTreeHelper.GetParent(obj);
            }
            return null;
        }

        private static T FindVisualParent<T>(DependencyObject obj) where T : DependencyObject
        {
            while (obj != null)
            {
                if (obj is T parent)
                    return parent;
                obj = VisualTreeHelper.GetParent(obj);
            }
            return null;
        }

        // Drag-and-drop support.
        private void TaskCard_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (sender is Border card)
                {
                    if ((e.GetPosition(card) - _dragStartPoint).Length > 10)
                    {
                        if (card.Tag is TaskCardTagData tagData)
                        {
                            var data = new TaskData
                            {
                                Text = tagData.TaskTextBlock.Text,
                                Category = tagData.Category,
                                ID = tagData.TaskID
                            };
                            DragDrop.DoDragDrop(card, data, DragDropEffects.Copy);
                        }
                    }
                }
            }
        }

        private void TaskCard_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(sender as IInputElement);
        }

        // Back button.
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            MainTabControl.SelectedIndex = 0;
        }

        // Handle drop on a day panel.
        private void DayPanel_Drop(object sender, DragEventArgs e)
        {
            if (sender is Border dayBorder && dayBorder.Child is Panel dayStack)
            {
                if (e.Data.GetDataPresent(typeof(TaskData)))
                {
                    TaskData data = (TaskData)e.Data.GetData(typeof(TaskData));
                    foreach (var child in dayStack.Children)
                    {
                        if (child is Border card && card.Tag is TaskCardTagData tagData)
                        {
                            if (tagData.TaskID == data.ID)
                                return;
                        }
                    }
                    Border weeklyCard = CreateTaskCard(data);
                    if (weeklyCard.Tag is TaskCardTagData tct)
                        tct.IsDayPanel = true;
                    weeklyCard.PreviewMouseLeftButtonDown += TaskCard_PreviewMouseLeftButtonDown;
                    weeklyCard.PreviewMouseMove += TaskCard_MouseMove;
                    dayStack.Children.Add(weeklyCard);

                    string dayName = GetDayNameFromPanel(dayBorder);
                    var existingTask = _savedTasks.Find(t => t.ID == data.ID);
                    if (existingTask != null)
                        existingTask.DayAssignment = dayName;
                    else
                    {
                        existingTask = new TaskItem
                        {
                            ID = data.ID,
                            Text = data.Text,
                            OriginalCategory = data.Category,
                            DayAssignment = dayName,
                            MarkerStates = new List<bool>(taskMarkerStates.ContainsKey(data.ID) ? taskMarkerStates[data.ID] : new List<bool> { false })
                        };
                        _savedTasks.Add(existingTask);
                    }
                    SaveAllTasksToDisk();
                }
            }
        }

        // Helper: Get day name from a day panel.
        private string GetDayNameFromPanel(Border dayPanel)
        {
            if (dayPanel == MondayPanel) return "Monday";
            if (dayPanel == TuesdayPanel) return "Tuesday";
            if (dayPanel == WednesdayPanel) return "Wednesday";
            if (dayPanel == ThursdayPanel) return "Thursday";
            if (dayPanel == FridayPanel) return "Friday";
            if (dayPanel == SaturdayPanel) return "Saturday";
            if (dayPanel == SundayPanel) return "Sunday";
            return string.Empty;
        }

        // Allow drop into Task Pool.
        private void TaskPool_Drop(object sender, DragEventArgs e)
        {
            if (sender is ListBox lb)
            {
                if (e.Data.GetDataPresent(typeof(TaskData)))
                {
                    TaskData data = (TaskData)e.Data.GetData(typeof(TaskData));
                    foreach (var item in lb.Items)
                    {
                        if (item is Border b && b.Tag is TaskCardTagData tcd && tcd.TaskID == data.ID)
                            return;
                    }
                    Border poolCard = CreateTaskCard(data);
                    poolCard.PreviewMouseLeftButtonDown += TaskCard_PreviewMouseLeftButtonDown;
                    poolCard.PreviewMouseMove += TaskCard_MouseMove;
                    lb.Items.Add(poolCard);
                    SaveAllTasksToDisk();
                }
            }
        }

        // Enable dragging for items in Task Pool.
        private void TaskPoolItem_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (sender is ListBox lb && lb.SelectedItem is Border card)
                {
                    if (card.Tag is TaskCardTagData tagData)
                    {
                        var data = new TaskData
                        {
                            Text = tagData.TaskTextBlock.Text,
                            Category = tagData.Category,
                            ID = tagData.TaskID
                        };
                        DragDrop.DoDragDrop(card, data, DragDropEffects.Copy);
                    }
                }
            }
        }
    }
}
