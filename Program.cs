

/*

* V can add SQL to save the queries
* Exception Handling
* Can use Akka to send messaged from TaskManager to MessageService(between different components)
    1. TaskManager
    2. MessageService
    3. DBManager
    Each will be having their communication actor
* Can use an API which will generate random names for tasks
* Check where we could have used and Interface
 
** For each CRUD operation, define an event, and when this event will be triggered, use asyc call to save into a file **

*/

using System;
using System.Dynamic;
//using System.Windows.Forms;     // not working even after adding the reference

namespace TaskManager
{

    public delegate void TaskOperatorEventHandler(object source, TaskItem taskItem);

    public class Program
    {
        public static void Main(string[] args)
        {
            TaskManager taskManager = new TaskManager();

            MessageService messageService = new MessageService();

            TaskItemRepository taskItemRepository = new TaskItemRepository();   

            // Creating a taskItem   
            //taskManager.TaskOperatorEvent += messageService.UpdateUserWithFileAndConsole;

            //Console.WriteLine("Starting taskItem creation...");
            //Thread.Sleep(2000);

            //taskManager.CreateTask("My taskItem 1");
            //taskManager.CreateTask("My taskItem 2");
            //taskManager.CreateTask("My taskItem 3");
            //taskManager.CreateTask("My taskItem 4");
            ////taskManager.CreateTask("\n");
            ////taskManager.CreateTask("************************\n");
            //taskManager.TaskOperatorEvent -= messageService.UpdateUserWithFileAndConsole;

            // Deleting a taskItem
            //taskManager.TaskOperatorEvent += messageService.OnTaskDeleted;
            //taskManager.DeleteTask();
            //taskManager.TaskOperatorEvent -= messageService.OnTaskDeleted;

            // Updating a task
            //taskManager.TaskOperatorEvent += messageService.OnTaskUpdated;
            //taskManager.UpdateTask("My taskItem 4", "My taskItem 4.1");
            //taskManager.TaskOperatorEvent -= messageService.OnTaskUpdated;

            // Reading a task
            /*
             Algorithm: 
                *. Send the string of the task to be searched for 
                *. Search the task and Return the Id of the task
                *. If the task doesn't found, send the empty TaskItem to Subscriber, so that the user can be notified about the result
                * 

             */
            //taskManager.TaskOperatorEvent += messageService.GetTaskAndUpdateConsole;
            //taskManager.GetTaskItemId("My taskItem 3");
            //Console.ReadLine();
        }
    }

    // Publisher
    public class TaskManager
    {
        public event TaskOperatorEventHandler TaskOperatorEvent;

        TaskItemRepository taskItemRepository = new TaskItemRepository();

        private List<TaskItem> taskList = new List<TaskItem>();

        #region Delete new task

        public void DeleteTask()
        {
            Console.WriteLine("Enter the taskItem name to be deleted");
            string taskNameToDelete = Console.ReadLine();    // could be empty - try-catch
            Console.WriteLine("Initiating Delete...");

            TaskItem foundTask = taskList.Where(_ => _.Name.Equals(taskNameToDelete)).FirstOrDefault();

            bool isDeleted = taskList.Remove(foundTask);    // foundTask could be null

            if (isDeleted)
            {
                OnTaskDeleted(foundTask);

            }
            else
            {
                Console.WriteLine("Enter a valid taskItem");
            }
        }

        public void OnTaskDeleted(TaskItem task)
        {
            if (TaskOperatorEvent != null)
            {
                TaskOperatorEvent(this, task);
            }
        }

        #endregion

        #region Create new task
        public void CreateTask(string newTaskName)
        {
            //Console.WriteLine("Enter new taskItem name");
            //string newTaskName = Console.ReadLine();

            TaskItem newTask = new TaskItem() { Name = newTaskName, Id = Guid.NewGuid() };
            newTask.taskOperationsString = newTask.GetTaskOperationsString(TaskOperations.Create);
            Console.WriteLine(newTask.ToString());
            
            //Console.WriteLine("Starting taskItem creation...");
            
            taskList.Add(newTask);

            //Thread.Sleep(2000);

            OnTaskCreated(newTask);

        }

        public void OnTaskCreated(TaskItem task)
        {
            if (TaskOperatorEvent != null)
            {
                TaskOperatorEvent(this, task);
            }
        }

        #endregion

        #region Update a task
        public void UpdateTask(string oldTask, string newTask)
        {
            TaskItem foundTask = taskList.Where(_ => _.Name.Equals(oldTask)).FirstOrDefault();

            var foundIndex = taskList.FindIndex(_ => _.Name.Equals(oldTask));

            if (foundIndex != -1)
            {
                taskList[foundIndex].Name = newTask;    
            }

            OnTaskUpdated(oldTask, newTask);
        }

        public void OnTaskUpdated(string oldTask, string newTask)
        {
            if (TaskOperatorEvent != null)
            {
                TaskOperatorEvent(this, new TaskItem() { Name = oldTask });    // Suppose here V need to send the 2 tasks, old and the new one to the subscriber. But according to the Delegate V have defined, V can send only 1 task. Then how can we send the 2 tasks to the subscrier. Should we define a new message which will contain all the values which are required to be send to the subscriber?
            }
        }

        #endregion

        #region Reading a Task
        public async Task GetTaskItemId(string taskName)
        {
            var foundTaskId = await taskItemRepository.GetTaskId(taskName);           // can make this asynchronous
            //MessageBox
            Console.WriteLine(foundTaskId);

            //if (foundTask != null)
            //{
            //    return foundTask;
            //}

            //return null;
        }

        public void OnTaskFound(TaskItem taskItem)
        {
            if (TaskOperatorEvent != null)
            {
                TaskOperatorEvent(this, taskItem);
            }
        }

        #endregion

    }

    // Subscriber
    public class MessageService
    {

        public void UpdateUserWithFileAndConsole(object source, TaskItem taskItem)
        {
            var taskItemRepository = new TaskItemRepository();
            TaskCompletionStatus taskCompletionStatus = taskItemRepository.SaveIntoFile(taskItem, taskItem.taskOperationsString).Result;  // ** do this async way    // ** Calling TaskItemRepository is not the work of this class. This class is intended for Message only
            
            switch (taskCompletionStatus)
            {
                case TaskCompletionStatus.Success:
                    PrintToConsole(taskItem.taskOperationsString, taskItem);
                    break;
                case TaskCompletionStatus.Failure:
                    string taskFailureString = taskItem.GetTaskCompletionStatusString(TaskCompletionStatus.Failure);
                    PrintToConsole(taskFailureString, taskItem);
                    break;
            }
        }

        private void PrintToConsole(string taskStausString, TaskItem taskItem)
        {
            Console.WriteLine(taskStausString + taskItem?.Name);
        }

        public void GetTaskAndUpdateConsole(object source, TaskItem taskItem)
        {
            TaskItemRepository taskItemRepository = new TaskItemRepository();

            var TaskId = taskItemRepository.GetTaskId(taskItem.Name);
        }

    }

    public class TaskItemRepository
    {
        //string docPath = 
        string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TaskManagerOperations.txt");

        public async Task<TaskCompletionStatus> SaveIntoFile(TaskItem taskItem, string taskStatusString)     // what if this method fails to save to a file
        {   
            try
            {
                using (StreamWriter outputFile = new StreamWriter(filePath, true))
                {
                    DateTime currentTime = DateTime.Now;
                    await outputFile.WriteLineAsync(taskItem.Id + " , " + taskStatusString + taskItem.Name + " , " + currentTime.ToString());
                }
                return TaskCompletionStatus.Success;
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync("Can't write to the file: " + ex.Message);
                return TaskCompletionStatus.Failure;
            }
        }

        public async Task<string[]> GetTaskItems()
        {
            var foundTasks = await File.ReadAllTextAsync(filePath);

            string[] taskItems = foundTasks.Split('\n');

            return taskItems;
        }

        public async Task<string> GetTaskId(string taskName)
        {
            try
            {
                //var foundTasks = await File.ReadAllTextAsync(filePath);

                /*
                    * V will be getting a string of all task items
                    * Split the string based on "\n". Now we are having individual task items as strings. Split will return an array
                    * TR this array of individual taskItem, 
                        * Split the current taskItem based on "," 
                        * Remove the spaces around each text
                        * Compare the Name of the current task item with the item V want to find
                        * If matched, return the id of the current item
                */

                //string[] taskItems = foundTasks.Split("\n");
                string[] taskItems = await GetTaskItems();

                foreach (var taskItem in taskItems)
                {
                    string[] currentTaskItemDetails = taskItem.Split(",");

                    if (currentTaskItemDetails[1].Trim().Contains(taskName))
                    {
                        //await Console.Out.WriteLineAsync();
                        return currentTaskItemDetails[0].Trim();
                    }
                }


            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
            }

            return null;
        }
    }

    public class Utility
    {
        public List<TaskItem> FindTask(List<TaskItem> tasksList, TaskItem task)
        {
            return null;
        }
    }

    public class TaskItem : EventArgs
    {
        public string Name;

        public Guid Id;

        public string taskOperationsString;

        public string GetTaskOperationsString(TaskOperations taskOperations)    // which is the better approach, curate the result string till the end of the method and then return it, or once a case is matched, returned immediately.
        {
            switch (taskOperations)
            {
                case TaskOperations.Create:
                    return "New task created: ";
                //break;
                case TaskOperations.Delete:
                    return "Task deleted: ";
                case TaskOperations.Update:
                    return "Task updated: ";
                case TaskOperations.Read:
                    return "Task reading complete.";
                default:
                    return "Unknown task operation.";
            }
        }

        public string GetTaskCompletionStatusString(TaskCompletionStatus taskCompletionStatus)
        {
            switch (taskCompletionStatus)
            {
                case TaskCompletionStatus.Success:
                    return "Task completed successfully.";
                case TaskCompletionStatus.Failure:
                    return "Task failed.";
                default:
                    return "Unknown task status";
            }
        }

        public override string ToString()
        {
            return $"Id: {Id} \tTask: {Name}";
        }

    }

    public enum TaskCompletionStatus
    {
        Success,
        Failure
    }

    public enum TaskOperations
    {
        Create, 
        Delete, 
        Update,
        Read,
    }
}
























