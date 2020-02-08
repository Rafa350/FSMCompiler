﻿namespace MikroPicDesigns.FSMCompiler.v1.Loader {

    using System;
    using System.Xml;
    using MikroPicDesigns.FSMCompiler.v1.Model;
    using MikroPicDesigns.FSMCompiler.v1.Model.Activities;

    public class XmlLoader {

        public Machine Load(string fileName) {

            XmlDocument document = ReadDocument(fileName);
            return ProcessMachineNode(document.SelectSingleNode("machine"));
        }

        private static XmlDocument ReadDocument(string fileName) {

            XmlDocument document = new XmlDocument();
            document.Load(fileName);
            return document;
        }

        private static string GetAttribute(XmlNode node, string name) {

            if (node.Attributes[name] == null)
                return null;
            else
                return node.Attributes[name].Value;
        }

        /// <summary
        /// Crea un objecte Machine a partir d'un node XML
        /// </summary>
        /// <param name="machineNode">El node a procesar.</param>
        /// <returns>L'objecte 'machine' creat.</returns>
        /// 
        private Machine ProcessMachineNode(XmlNode machineNode) {

            string machineName = GetAttribute(machineNode, "name");
            if (String.IsNullOrEmpty(machineName))
                throw new Exception("No se especifico el atributo 'name'");

            string startStateName = GetAttribute(machineNode, "start");
            if (String.IsNullOrEmpty(startStateName))
                throw new Exception("No se especifico el atributo 'start'");

            Machine machine = new Machine(machineName);

            XmlNode initializeNode = machineNode.SelectSingleNode("initialize");
            if (initializeNode != null)
                machine.InitializeAction = ProcessActionNode(initializeNode, machine);

            XmlNode terminateNode = machineNode.SelectSingleNode("terminate");
            if (terminateNode != null)
                machine.TerminateAction = ProcessActionNode(terminateNode, machine);

            // Procesa cada estat i asigna els parametres
            //
            foreach (XmlNode stateNode in machineNode.SelectNodes("state")) {
                ProcessStateNode(stateNode, machine);
            }

            startStateName = startStateName.Replace(":", "");
            machine.Start = GetState(machine, startStateName);

            return machine;
        }

        private void ProcessStateNode(XmlNode stateNode, Machine machine) {

            string stateName = null;
            if (stateNode.ParentNode.Name == "state")
                stateName = GetAttribute(stateNode.ParentNode, "name");
            stateName += GetAttribute(stateNode, "name");

            State state = GetState(machine, stateName);

            XmlNode onEnterNode = stateNode.SelectSingleNode("enter");
            if (onEnterNode != null)
                state.EnterAction = ProcessActionNode(onEnterNode, machine);

            XmlNode onExitNode = stateNode.SelectSingleNode("exit");
            if (onExitNode != null)
                state.ExitAction = ProcessActionNode(onExitNode, machine);

            foreach (XmlNode transitionNode in stateNode.SelectNodes("transition")) {
                Transition transition = ProcessTransitionNode(transitionNode, machine);
                state.AddTransition(transition);
            }

            foreach (XmlNode childStateNode in stateNode.SelectNodes("state"))
                ProcessStateNode(childStateNode, machine);
        }

        private Model.Action ProcessActionNode(XmlNode actionNode, Machine machine) {

            Model.Action action = new Model.Action();

            foreach (XmlNode node in actionNode.ChildNodes) {
                switch (node.Name) {
                    case "code":
                    case "inline":
                        action.AddActivity(ProcessCodeActivityNode(node, machine));
                        break;
                    case "call":
                    case "command":
                        action.AddActivity(ProcessCallActivityNode(node, machine));
                        break;
                }
            }

            return action;
        }

        private Activity ProcessCodeActivityNode(XmlNode inlineActionNode, Machine machine) {

            CodeActity actifity = new CodeActity();
            actifity.Text = inlineActionNode.InnerText;

            return actifity;
        }

        private Activity ProcessCallActivityNode(XmlNode commandActionNode, Machine machine) {

            CallActivity activity = new CallActivity();
            activity.MethodName = GetAttribute(commandActionNode, "name");

            return activity;
        }

        private Transition ProcessTransitionNode(XmlNode transitionNode, Machine machine) {

            // Obte el nom
            //
            string transitionName = GetAttribute(transitionNode, "name");
            if (String.IsNullOrEmpty(transitionName))
                throw new Exception("No se especifico el atributo 'name'");

            Transition transition = new Transition(transitionName);

            // Obte la guarda
            //
            string condition = GetAttribute(transitionNode, "guard");
            if (!String.IsNullOrEmpty(condition))
                transition.Guard = new Guard(condition);

            // Obte el nou estat
            //
            string name = GetAttribute(transitionNode, "state");
            if (System.String.IsNullOrEmpty(name))
                transition.Mode = TransitionMode.InternalLoop;

            else {
                if (name == "*")
                    transition.Mode = TransitionMode.Pop;

                else {
                    if (name.Contains(":"))
                        name = name.Replace(":", "");
                    else {
                        XmlNode node = transitionNode.ParentNode.ParentNode;
                        while (node.Name == "state") {
                            name = System.String.Format("{0}{1}", GetAttribute(node, "name"), name);
                            node = node.ParentNode;
                        }
                    }

                    transition.NextState = GetState(machine, name);
                    transition.Mode = TransitionMode.Jump;
                }
            }

            if (transitionNode.HasChildNodes)
                transition.Action = ProcessActionNode(transitionNode, machine);

            return transition;
        }

        /// <summary>
        /// Obte l'estat especificat. Si no existeix, el crea
        /// </summary>
        /// <param name="machine">La maquina.</param>
        /// <param name="name">Nom de l'estat.</param>
        /// <returns>L'estat.</returns>
        /// 
        private static State GetState(Machine machine, string name) {

            State ev = machine.GetState(name, false);
            if (ev == null) {
                ev = new State(name);
                machine.AddState(ev);
            }
            return ev;
        }

    }
}
