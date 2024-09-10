

/*

* V can add SQL to save the queries
* Exception Handling
* Can use Akka to send messaged from TaskManager to MessageService
 
** For each CRUD operation, define an event, and when this event will be triggered, use asyc call to save into a file **

*/

using System;

namespace TaskManager
{

    public delegate void TaskOperatorEventHandler(object source, TaskItem taskItem);

    public class Program
    {
        public static void Main(string[] args)
        {

            TaskManager taskManager = new TaskManager();

            MessageService messageService = new MessageService();

            // Creating a taskItem   
            taskManager.TaskOperatorEvent += messageService.UpdateUserWithFileAndConsole;

            Console.WriteLine("Starting taskItem creation...");
            Thread.Sleep(2000);

            taskManager.CreateTask("My taskItem 1");
            taskManager.CreateTask("My taskItem 2");
            taskManager.CreateTask("My taskItem 3");
            taskManager.CreateTask("My taskItem 4");
            taskManager.CreateTask("\n");
            //taskManager.CreateTask("************************\n");
            taskManager.TaskOperatorEvent -= messageService.UpdateUserWithFileAndConsole;

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
                1. Send the string of the task to be searched for 
                2. Return the complete object
                3. If the task doesn't found, send the empty TaskItem to Subscriber, so that the user can be notified about the result
             */
            //taskManager.TaskOperatorEvent += messageService.OnTaskFound;
            //taskManager.GetTaskItem("My taskItem 3");

        }
    }

    // Publisher
    public class TaskManager
    {
        public event TaskOperatorEventHandler TaskOperatorEvent;

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
        public TaskItem GetTaskItem(string taskName)
        {
            var foundTask = taskList.Find(_ => _.Name.Equals(taskName));

            OnTaskFound(foundTask);             // can make this asynchronous

            if (foundTask != null)
            {
                return foundTask;
            }

            return null;
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
            TaskCompletionStatus taskCompletionStatus = SaveIntoFile(taskItem, taskItem.taskOperationsString).Result;  // ** do this async way
            
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

        private async Task<TaskCompletionStatus> SaveIntoFile(TaskItem taskItem, string taskStatusString)     // what if this method fails to save to a file
        {
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            try
            {
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "TaskManagerOperations.txt"), true))
                {
                    DateTime currentTime = DateTime.Now;
                    await outputFile.WriteLineAsync(taskItem.Id + "\t" + taskStatusString + taskItem.Name + "\t" + currentTime.ToString());
                }
                return TaskCompletionStatus.Success;
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync("Can't write to the file: " + ex.Message);
                return TaskCompletionStatus.Failure;
            }
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
























