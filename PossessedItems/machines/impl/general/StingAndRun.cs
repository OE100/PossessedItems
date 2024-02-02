using GameNetcodeStuff;
using PossessedItems.machines.def;
using PossessedItems.networking;
using UnityEngine;
using UnityEngine.AI;

namespace PossessedItems.machines.impl.general;

public class StingAndRun : MonoBehaviour
{
    private class Data
    {
        // original object data
        public GrabbableObject Grabbable;
        public Quaternion OriginalRotation { get; set; }
        
        // ai components
        public NavMeshAgent Agent;
        public NavMeshPath Path;
        public readonly NavMeshAgent DemoAgent = Utils.EnemyPrefabRegistry[typeof(CentipedeAI)].GetComponent<NavMeshAgent>();
        public bool Inside = true;
        public GameObject TargetAINode { get; set; }
        public PlayerControllerB TargetPlayer { get; set; }
        
        public float TimeUntilRetarget = 0f;
        public const float RetargetTime = 0.5f;
    }
    
    private enum State
    {
        Initial,
        ChooseNode,
        GoToNode,
        ChooseTarget,
        GoToTarget,
        SneakAttack,
        Wait
    }

    private Data _data;
    private FiniteStateMachine<State, Data> _finiteStateMachine;

    private void Awake()
    {
        _data = new Data();
        Initialize();
        _finiteStateMachine = new FiniteStateMachine<State, Data>(_data);
        AssignFunctions();
    }

    private void Update()
    {
        _finiteStateMachine?.Tick();
    }

    private void Initialize()
    {
        // get the original object rotation
        _data.OriginalRotation = transform.rotation;
        
        // get the original object script
        if (!TryGetComponent(out _data.Grabbable)) DestroyImmediate(this);
        
        // create a new NavMeshAgent
        if (!TryGetComponent(out _data.Agent)) (_data.Agent = gameObject.AddComponent<NavMeshAgent>()).enabled = false;
        
        SetAgentDefaults();
    }

    private void SetAgentDefaults()
    {
        _data.Agent.agentTypeID = _data.DemoAgent.agentTypeID;
        _data.Agent.baseOffset = _data.DemoAgent.baseOffset;
        _data.Agent.acceleration = _data.DemoAgent.acceleration / 2;
        _data.Agent.angularSpeed = _data.DemoAgent.angularSpeed / 2;
        _data.Agent.stoppingDistance = _data.DemoAgent.stoppingDistance / 2;
        _data.Agent.autoBraking = _data.DemoAgent.autoBraking;
        _data.Agent.radius = _data.DemoAgent.radius / 2;
        _data.Agent.height = _data.DemoAgent.height / 2;
        _data.Agent.obstacleAvoidanceType = _data.DemoAgent.obstacleAvoidanceType;
        _data.Agent.avoidancePriority = _data.DemoAgent.avoidancePriority;
        _data.Agent.autoTraverseOffMeshLink = _data.DemoAgent.autoTraverseOffMeshLink;
        _data.Agent.autoRepath = _data.DemoAgent.autoRepath;
        _data.Agent.areaMask = _data.DemoAgent.areaMask;
    }

    private void AssignFunctions()
    {
        _finiteStateMachine.AddPreTickAction(PreTick);
        _finiteStateMachine.AddAction(State.Initial, Initial);
        _finiteStateMachine.AddAction(State.ChooseNode, ChooseNode);
        _finiteStateMachine.AddAction(State.GoToNode, GoToNode);
        _finiteStateMachine.AddAction(State.ChooseTarget, ChooseTarget);
        _finiteStateMachine.AddAction(State.GoToTarget, GoToTarget);
        _finiteStateMachine.AddAction(State.SneakAttack, SneakAttack);
        _finiteStateMachine.AddAction(State.Wait, Wait);
    }

    private void PreTick(State previousState, State currentState, Data data)
    {
        if (!currentState.Equals(State.Wait) && data.Grabbable.playerHeldBy)
            _finiteStateMachine.SwitchStates(State.Wait);
        
        // todo: think about targeting the door and teleporting outside to chase the player
        if (data.TargetPlayer && data.TargetPlayer.isInsideFactory != data.Inside)
        {
            data.TargetPlayer = null;
            _finiteStateMachine.SwitchStates(State.ChooseTarget);
        }
    } 
    
    private static State Initial(State previousState, Data data)
    {
        // find the closest valid NavMesh to lock unto
        if (!NavMesh.SamplePosition(data.Grabbable.transform.position, out var hit, 10f, NavMesh.AllAreas))
            return State.Wait;
        
        PossessedItemsBehaviour.Instance.SetGrabbableStateServerRpc(data.Grabbable.NetworkObject, false);

        data.Agent.Warp(hit.position);
        
        data.Agent.enabled = true;
        
        return State.ChooseNode;
    }
    
    private static State ChooseNode(State previousState, Data data)
    {
        var nodes = data.Inside ? MachineUtils.InsideAINodes : MachineUtils.OutsideAINodes;
        
        if (!MachineUtils.AveragePlayerPosition(data.Inside, out var pos))
            pos = data.Grabbable.transform.position;
        
        data.TargetAINode = nodes
            .OrderByDescending(node => Vector3.Distance(pos, node.transform.position))
            .First();
        
        return data.TargetAINode ? State.GoToNode : State.ChooseNode;
    }
    
    private static State GoToNode(State previousState, Data data)
    {
        if (!previousState.Equals(State.GoToNode))
        {
            data.Agent.speed = data.DemoAgent.speed / 4;
            NavMesh.SamplePosition(data.TargetAINode.transform.position, out var hit, float.MaxValue, NavMesh.AllAreas);
            data.Agent.CalculatePath(hit.position, data.Path = new NavMeshPath());
        }
        else if (Vector3.Distance(data.Path.corners[^1], data.Grabbable.transform.position) < 10f)
            return State.ChooseTarget;

        return State.ChooseTarget;
    }
    
    private static State ChooseTarget(State previousState, Data data)
    {
        if (!previousState.Equals(State.ChooseTarget))
            data.Agent.speed = data.DemoAgent.speed / 4;
        
        var players = Utils.GetActivePlayers(data.Inside);
        if (players.Count == 0) return State.ChooseNode;
        players = players
            .OrderByDescending(player => -1 * Vector3.Distance(player.transform.position, data.Grabbable.transform.position))
            .ToList();
        
        foreach (var player in players)
        {
            data.Agent.CalculatePath(player.transform.position, data.Path = new NavMeshPath());
            if (data.Path.status.Equals(NavMeshPathStatus.PathComplete))
            {
                data.TargetPlayer = player;
                return State.GoToTarget;
            }
        }
        return State.ChooseNode;
    }
    
    private static State GoToTarget(State previousState, Data data)
    {
        var position = data.Grabbable.transform.position;
        
        // if close enough to player go to sneak attack
        if (Vector3.Distance(position, data.TargetPlayer.transform.position) < 30f)
            return State.SneakAttack;
        
        // else if reached end of the path recalculate path
        if (Vector3.Distance(position, data.Path.corners[^1]) < 10f)
            return State.ChooseTarget;
        
        // else keep going towards target
        return State.GoToTarget;
    }
    
    private static State SneakAttack(State previousState, Data data)
    {
        if (!previousState.Equals(State.SneakAttack))
            data.Agent.speed = data.DemoAgent.speed * 1.5f;

        var pp = data.TargetPlayer.transform.position;

        if (Vector3.Distance(pp, data.Agent.transform.position) < 2f)
        {
            data.TargetPlayer.DamagePlayer(5);
            return State.ChooseNode;
        }
        
        data.TimeUntilRetarget -= Time.deltaTime;
        
        if (data.TimeUntilRetarget <= 0f)
        {
            data.Agent.CalculatePath(pp, data.Path = new NavMeshPath());
            data.Agent.SetPath(data.Path);
            data.TimeUntilRetarget = Data.RetargetTime;
        }

        return State.SneakAttack;
    }
    
    private static State Wait(State previousState, Data data)
    {
        switch (previousState)
        {
            // if was in wait state and held by player remain in wait state
            case State.Wait when data.Grabbable.playerHeldBy:
                return State.Wait;
            
            // if wasn't in wait turn off all components and turn back on the item
            case State.Wait:
                return State.Initial;
            
            // else if not held by player restart the state machine
            default:
                data.Agent.enabled = false;
                data.Grabbable.transform.rotation = data.OriginalRotation;
                PossessedItemsBehaviour.Instance.SetGrabbableStateServerRpc(data.Grabbable.NetworkObject, true);
                return State.Wait;
        }
    }
}