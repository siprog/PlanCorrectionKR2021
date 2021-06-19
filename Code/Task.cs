using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanValidation1
{
    /// <summary>
    /// Represent the actual task. 
    /// </summary>
    class Task
    {
        /// <summary>
        /// Name of this task. 
        /// </summary>
        public Term TaskInstance;
        /// <summary>
        /// Action vector describes to which actions this task deocmposes to. 
        /// </summary>
        private bool[] ActionVector;
        /// <summary>
        /// Type of this task. 
        /// </summary>
        public TaskType TaskType { get; }
        /// <summary>
        /// The starting position of this task. 
        /// For tasks that have subtasks this is just first one in ActionVector. For empty subtasks this is the slot, on which it's true-0,5. 
        /// </summary>
        public double StartIndex;
        /// <summary>
        /// Final position of this task. 
        /// For tasks that have subtasks this is just last one in ActionVector. For empty subtasks this is the slot, on which it's true-0,5. 
        /// </summary>
        public double EndIndex;
        /// <summary>
        /// Itertaion number in which this task was created. 
        /// </summary>
        public int Iteration;

        /// <summary>
        /// Indexes of actions that must be deleted in order for this task to be valid
        /// Warning: Action indexes are calculated from 1 not 0. Explanation in AtomTlnMaker. 
        /// </summary>
        public List<int> DeleteActions;

        /// <summary>
        /// For each condition of this task (term) there is an atom timeline and an index of last action that provides support for it but this task does not decompose to it.  
        /// </summary>
        public List<Tuple<Term, List<int>, int>> Supports;

        /// <summary>
        /// returns action vector for this task. 
        /// </summary>
        /// <returns></returns>
        public bool[] GetActionVector()
        {
            return ActionVector;
        }

        /// <summary>
        /// Sets actions vector for this task. 
        /// </summary>
        /// <param name="actionvector"></param>
        public void SetActionVector(bool[] actionvector)
        {
            ActionVector = actionvector;
            UpdateStartIndex();
            UpdateEndIndex();
        }

        private void UpdateEndIndex()
        {
            for (int i = ActionVector.Length - 1; i >= 0; i--)
            {
                if (ActionVector[i])
                {
                    EndIndex = i;
                    break;
                }
            }
        }

        /// <summary>
        /// For empty subtasks this changes nothing. 
        /// </summary>
        private void UpdateStartIndex()
        {
            for (int i = ActionVector.Length - 1; i >= 0; i--)
            {
                if (ActionVector[i])
                {
                    StartIndex = i;
                    break;
                }
            }
        }

        public Task(Task t)
        {
            this.TaskInstance = t.TaskInstance;
            this.ActionVector = t.ActionVector;
            this.TaskType = t.TaskType;
        }

        public Task(Term taskInstance, int size, TaskType type)
        {
            TaskInstance = taskInstance;
            ActionVector = new bool[size];
            TaskType = type;
        }

        /// <summary>
        /// This is used for creating empty tasks. 
        /// </summary>
        /// <param name="taskInstance"></param>
        /// <param name="size"></param>
        /// <param name="type"></param>
        /// <param name="StartIndex"></param>
        /// <param name="EndIndex"></param>
        public Task(Term taskInstance, int size, TaskType type, double StartIndex, double EndIndex)
        {
            TaskInstance = taskInstance;
            ActionVector = new bool[size];
            TaskType = type;
            this.StartIndex = StartIndex;
            this.EndIndex = EndIndex;
            if (EndIndex < StartIndex)
            {
                Console.WriteLine("Error: Endindex is smaller than startindex");
            }
        }

        /// <summary>
        /// This is used for creating empty tasks. 
        /// </summary>
        /// <param name="taskInstance"></param>
        /// <param name="size"></param>
        /// <param name="type"></param>
        /// <param name="StartIndex"></param>
        /// <param name="EndIndex"></param>
        public Task(Term taskInstance, bool[] vector, TaskType type, double StartIndex, double EndIndex, List<int> deleteActions, List<Tuple<Term, List<int>, int>> supports) : this(taskInstance, vector, type, StartIndex, EndIndex)
        {
            this.DeleteActions = deleteActions;
            this.Supports = supports;
        }

        public Task(Term taskInstance, bool[] vector, TaskType type)
        {
            TaskInstance = taskInstance;
            ActionVector = vector;
            TaskType = type;
            UpdateEndIndex();
            UpdateStartIndex();
        }

        public Task(Term taskInstance, bool[] vector, TaskType type, double StartIndex, double EndIndex)
        {
            TaskInstance = taskInstance;
            ActionVector = vector;
            TaskType = type;
            this.StartIndex = StartIndex;
            this.EndIndex = EndIndex;
        }

        public Task(Action a, int size, TaskType type)
        {
            TaskInstance = a.ActionInstance;
            ActionVector = new bool[size];
            TaskType = type;
        }

        /// <summary>
        /// Adds this task to instances of this task type and returns the task type
        /// </summary>
        /// <returns></returns>
        internal void AddToTaskType()
        {
            this.TaskType.AddInstance(this);
        }

        public double GetEndIndex()
        {
            return EndIndex;
        }

        public double GetStartIndex()
        {
            return StartIndex;
        }

        /// <summary>
        /// Describes this task in form of a string. 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = string.Join(",", TaskInstance.Variables.Select(x => x.Name).ToList());
            string vector = string.Join(",", ActionVector);
            String s = "Task:" + TaskInstance.Name + " Variables " + text + " TaskType " + TaskType.Name + " ActionVector " + vector + " StartIndex " + StartIndex + " EndIndex " + EndIndex;
            return s;
        }

        /// <summary>
        /// Returns a string which describes all actions this task does not deocmpose inot. 
        /// </summary>
        /// <returns></returns>
        public string UnusedActions()
        {
            string s = "";
            for(int i=0;i<ActionVector.Length; i++)
            {
                if (!ActionVector[i]) s = s + i + ",";
            }
            return s;
        }

        /// <summary>
        /// Returns the number of actions this task decomposes to. 
        /// </summary>
        /// <returns></returns>
        public int size()
        {
            int j = 0;
            for (int i = 0; i < ActionVector.Length; i++)
            {
                if (ActionVector[i]) j++;
            }
            return j;
        }
    }
}
