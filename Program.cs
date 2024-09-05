

/*
* For each CRUD operation, define an event, and when this event will be triggered, use asyc call to save into a file  
* V can add SQL to save the queries 
 
*/

using System.Linq;

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
            taskManager.TaskOperatorEvent += messageService.OnTaskCreated;

            Console.WriteLine("Starting taskItem creation...");
            Thread.Sleep(2000);

            taskManager.CreateTask("My taskItem 1");
            taskManager.CreateTask("My taskItem 2");
            taskManager.CreateTask("My taskItem 3");
            taskManager.CreateTask("My taskItem 4");
            //taskManager.CreateTask("************************\n");
            taskManager.TaskOperatorEvent -= messageService.OnTaskCreated;

            // Deleting a taskItem
            taskManager.TaskOperatorEvent += messageService.OnTaskDeleted;
            taskManager.DeleteTask();
            taskManager.TaskOperatorEvent -= messageService.OnTaskDeleted;

            // Updating a task
            taskManager.TaskOperatorEvent += messageService.OnTaskUpdated;
            taskManager.UpdateTask("My taskItem 4", "My taskItem 4.1");
            taskManager.TaskOperatorEvent -= messageService.OnTaskUpdated;

            // Reading a task
            /*
             Algorithm: 
                1. Send the string of the task to be searched for 
                2. Return the complete object
                3. If the task doesn't found, send the empty TaskItem to Subscriber, so that the user can be notified about the result
             */
            taskManager.TaskOperatorEvent += messageService.OnTaskFound;
            taskManager.GetTaskItem("My taskItem 3");

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

            TaskItem newTask = new TaskItem() { Name = newTaskName };
            
            Console.WriteLine("Starting taskItem creation...");
            
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

    public class MessageService
    {
        public void OnTaskCreated(object source, TaskItem task)
        {
            Console.WriteLine($"New taskItem : {task.Name} created");
        }

        public void OnTaskDeleted(object source, TaskItem task)
        {
            Console.WriteLine($"TaskItem : {task.Name} deleted successfully");
        }

        public void OnTaskUpdated(object source, TaskItem oldTask)
        {
            Console.WriteLine($"TaskItem : {oldTask.Name} is updated");
        }

        public void OnTaskFound(object source, TaskItem task)
        {
            if (task == null)
            {
                Console.WriteLine($"No task found for the given input");
            };

            Console.WriteLine($"Task item found: {task.Name}");
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
    }
}
























