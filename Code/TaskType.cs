using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanValidation1
{
    /// <summary>
    /// Type of a task. This is used to call Rules and tell them there are task instances ready. 
    /// </summary>
    class TaskType
    {
        /// <summary>
        /// List of rules that contains this type of task. 
        /// </summary>
        public List<Rule> Rules;
        public String Name; //Task name
        public int NumOfVariables; // Number of variables. So that load(X,Y) and load(X,Y,Z) are different TaskTypes
        /// <summary>
        /// List of instances of this task type
        /// </summary>
        public List<Task> Instances;
        /// <summary>
        /// Minimal task length of this task. This is updated "real time" based on current existing instances. 
        /// </summary>
        public int MinTaskLength;

        public TaskType(String name, int numOfVars)
        {
            this.Name = name;
            this.NumOfVariables = numOfVars;
            this.Instances = new List<Task>();
            this.Rules = new List<Rule>();
            MinTaskLength = 100000;
        }

        public TaskType(String name, int numOfVars, List<Task> instances, List<Rule> rules)
        {
            this.Rules = rules;
            this.Instances = instances;
            this.Name = name;
            this.NumOfVariables = numOfVars;
            MinTaskLength = 100000;
        }

        /// <summary>
        /// Sets mintask length to i if i is smaller than mintask length otherwise does nothing.
        /// Return true if value changed.
        /// </summary>
        /// <param name="i"></param>
        public bool SetMinTaskLengthIfSmaller(int i)
        {
            if (i < MinTaskLength)
            {
                MinTaskLength = i;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds new rule to list of rules that use this task type. 
        /// </summary>
        /// <param name="r"></param>
        public void AddRule(Rule r)
        {
            this.Rules.Add(r);
        }

        /// <summary>
        /// Adds instance to my list of instances. 
        /// </summary>
        /// <param name="t"></param>
        public void AddInstance(Task t)
        {
            if (!Instances.Contains(t)) Instances.Add(t);
        }

        /// <summary>
        /// Tells the rules that this task is now ready. If the rule is full (all tasks are ready it returns it otherwise returns null)
        /// </summary>
        /// <returns></returns>
        private Rule ActivateRule(Rule r, int i,int iteration)
        {
            bool fullyActivated = r.Activate(this, i, iteration);
            return fullyActivated ? r : null;
        }

        /// <summary>
        /// Tells the rules that this task is now ready. If the rules are full (all tasks are ready it returns them otherwise returns empty list)        /// 
        /// 
        /// </summary>
        /// <returns></returns>
        public List<Rule> ActivateRules(int iteration)
        {
            int instancesCount = Instances.Count;
            List<Rule> rulesReadyToGo = new List<Rule>();
            foreach (Rule r in Rules)
            {
                Rule r2 = ActivateRule(r, instancesCount,iteration);
                if (r2 != null) rulesReadyToGo.Add(r2);
            }
            return rulesReadyToGo;
        }

        /// <summary>
        /// Describes this task type in the form of a string. 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = string.Join(",", Rules.Select(x => x.MainTaskType.Name));
            string text2 = string.Join(",", Instances.Select(x => x.TaskInstance.Name));
            String s = "TaskType:" + this.Name + " Num of Variables " + NumOfVariables + " Rules: " + text + " Instances: " + text2;
            return s;
        }
    }
}
