using System.Collections;
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
        public ScanNodeProperties ScanNode;
        public Quaternion OriginalRotation { get; set; }
        
        // ai components
        public NavMeshAgent Agent;
        public NavMeshPath Path;
        public readonly NavMeshAgent DemoAgent = Utils.EnemyPrefabRegistry[typeof(CentipedeAI)].GetComponent<NavMeshAgent>();
        public bool Inside = true;

        // targeting
        public GameObject TargetAINode { get; set; }
        public PlayerControllerB TargetPlayer { get; set; }
        public float TimeUntilRetarget = 0f;
        public const float RetargetTime = 0.5f;
        public int AttackDamage;
        public int DamageRemaining = ModConfig.MaxDamageOverall.Value;

        // unstuck
        public Vector3 LastPosition { get; set; } = Vector3.zero;
        public float TimeUntilUnstuck = 0f;
        public const float UnstuckTime = 5f;
    }
    
    private enum State
    {
        Initial,
        ChooseNode,
        GoToNode,
        ChooseTarget,
        GoToTarget,
        Attack,
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

        // get the original object scan node
        _data.ScanNode = GetComponentInParent<ScanNodeProperties>();

        // create a new NavMeshAgent
        if (!TryGetComponent(out _data.Agent)) (_data.Agent = gameObject.AddComponent<NavMeshAgent>()).enabled = false;

        // calculate damage per hit
        _data.AttackDamage = Math.Clamp(
            Mathf.RoundToInt(ModConfig.BaseDamageOfItems.Value + ModConfig.UnitsOfWeightPerDamage.Value *
                (_data.Grabbable.itemProperties.weight - 1f) * 100), 
            ModConfig.BaseDamageOfItems.Value, ModConfig.MaxDamagePerHit.Value);
        
        SetAgentDefaults();
    }

    private void SetAgentDefaults()
    {
        var collider = _data.Grabbable.GetComponent<Collider>();
        _data.Agent.agentTypeID = _data.DemoAgent.agentTypeID;
        _data.Agent.baseOffset = collider ? collider.bounds.size.y : 0;
        _data.Agent.acceleration = _data.DemoAgent.acceleration;
        _data.Agent.angularSpeed = _data.DemoAgent.angularSpeed;
        _data.Agent.stoppingDistance = _data.DemoAgent.stoppingDistance;
        _data.Agent.autoBraking = _data.DemoAgent.autoBraking;
        _data.Agent.radius = collider ? Mathf.Max(collider.bounds.size.x, collider.bounds.size.z) * 2 : 0;
        _data.Agent.height = collider ? collider.bounds.size.y * 2 : 0;
        _data.Agent.obstacleAvoidanceType = _data.DemoAgent.obstacleAvoidanceType;
        _data.Agent.avoidancePriority = _data.DemoAgent.avoidancePriority;
        _data.Agent.autoTraverseOffMeshLink = _data.DemoAgent.autoTraverseOffMeshLink;
        _data.Agent.autoRepath = false;
        _data.Agent.areaMask = NavMesh.AllAreas;
    }

    private void AssignFunctions()
    {
        _finiteStateMachine.AddPreTickAction(PreTickStateCheck);
        _finiteStateMachine.AddPreTickAction(PreTickObstacleCheck);

        _finiteStateMachine.AddAction(State.Initial, Initial);
        _finiteStateMachine.AddAction(State.ChooseNode, ChooseNode);
        _finiteStateMachine.AddAction(State.GoToNode, GoToNode);
        _finiteStateMachine.AddAction(State.ChooseTarget, ChooseTarget);
        _finiteStateMachine.AddAction(State.GoToTarget, GoToTarget);
        _finiteStateMachine.AddAction(State.Attack, Attack);
        _finiteStateMachine.AddAction(State.Wait, Wait);
    }

    private void PreTickObstacleCheck(State previousState, State currentState, Data data)
    {
        // if the agent is enabled
        if (data.TimeUntilUnstuck <= 0f && currentState is State.GoToNode or State.GoToTarget or State.Attack && data.Agent.enabled)
        {
            data.TimeUntilUnstuck = Data.UnstuckTime; 
            // if the agent is stuck teleport it to a navmesh around itself
            if (Vector3.Distance(data.Agent.transform.position, data.LastPosition) < 0.5f)
            {
                if (NavMesh.SamplePosition(data.LastPosition + data.Agent.transform.forward * 5, out var navMeshHit, 4,
                        NavMesh.AllAreas))
                {
                    data.Agent.Warp(navMeshHit.position);
                    switch (currentState)
                    {
                        case State.GoToNode:
                            _finiteStateMachine.SwitchStates(State.ChooseNode);
                            break;
                        
                        case State.GoToTarget:
                            _finiteStateMachine.SwitchStates(State.ChooseTarget);
                            break;
                        
                        case State.Attack:
                            _data.TimeUntilRetarget = 0f;
                            break;
                    }
                }
            }
                
            // if the agent is close to an obstacle open the door
            if (Physics.Raycast(data.Agent.transform.position, data.Agent.transform.forward, out var raycastHit, 5f) && raycastHit.transform.gameObject.TryGetComponent<NavMeshObstacle>(out var obstacle))
                PossessedItemsBehaviour.Instance.StartCoroutine(DoorHandler(obstacle));
            
            data.LastPosition = data.Agent.transform.position;
        }
        
        data.TimeUntilUnstuck -= Time.deltaTime;
    }
    
    private void PreTickStateCheck(State previousState, State currentState, Data data)
    {
        // if the object is held by a player switch to wait state
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
        // check if was held by player
        if (data.Grabbable.playerHeldBy) data.Inside = data.Grabbable.playerHeldBy.isInsideFactory;
        // find the closest valid NavMesh to lock unto
        if (!NavMesh.SamplePosition(data.Agent.transform.position, out var hit, 10f, NavMesh.AllAreas))
            return State.Wait;
        
        PossessedItemsBehaviour.Instance.SetGrabbableStateServerRpc(data.Grabbable.NetworkObject, false);
        
        data.Agent.enabled = true;
        
        data.Agent.Warp(hit.position);
        
        data.LastPosition = data.Agent.transform.position;
        
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
    
    private State GoToNode(State previousState, Data data)
    {
        if (!previousState.Equals(State.GoToNode))
        {
            data.Agent.speed = data.DemoAgent.speed;
            NavMesh.SamplePosition(data.TargetAINode.transform.position, out var hit, float.MaxValue, NavMesh.AllAreas);
            data.Agent.CalculatePath(hit.position, data.Path = new NavMeshPath());
            data.Agent.SetPath(data.Path);
        }
        else if (data.DamageRemaining <= 0) BackToItem(true);
        else if (Vector3.Distance(data.Path.corners[^1], data.Agent.transform.position) < 10f)
            return State.ChooseTarget;

        return State.GoToNode;
    }
    
    private static State ChooseTarget(State previousState, Data data)
    {
        if (!previousState.Equals(State.ChooseTarget))
            data.Agent.speed = data.DemoAgent.speed;
        
        var players = Utils.GetActivePlayers(data.Inside);
        if (players.Count == 0) return State.ChooseNode;
        players = players
            .OrderByDescending(player => -1 * Vector3.Distance(player.transform.position, data.Agent.transform.position))
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
        var position = data.Agent.transform.position;
        
        // if close enough to player go to sneak attack
        if (Vector3.Distance(position, data.TargetPlayer.transform.position) < 30f)
            return State.Attack;
        
        // else if reached end of the path recalculate path
        if (Vector3.Distance(position, data.Path.corners[^1]) < 10f)
            return State.ChooseTarget;
        
        // else keep going towards target
        return State.GoToTarget;
    }
    
    private static State Attack(State previousState, Data data)
    {
        if (!previousState.Equals(State.Attack))
            data.Agent.speed = data.DemoAgent.speed;

        var pp = data.TargetPlayer.transform.position;

        if (Vector3.Distance(pp, data.Agent.transform.position) < 2f)
        {
            data.TargetPlayer.DamagePlayer(Math.Min(data.AttackDamage, data.DamageRemaining));
            data.DamageRemaining -= data.AttackDamage;
            return State.ChooseNode;
        }
        
        data.TimeUntilRetarget -= Time.deltaTime;
        
        if (data.TimeUntilRetarget <= 0f)
        {
            data.Agent.CalculatePath(pp, data.Path = new NavMeshPath());
            data.Agent.SetPath(data.Path);
            data.TimeUntilRetarget = Data.RetargetTime;
        }

        return State.Attack;
    }
    
    private State Wait(State previousState, Data data)
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
                BackToItem();
                return State.Wait;
        }
    }

    private static IEnumerator DoorHandler(NavMeshObstacle obstacle)
    {
        if (!obstacle.enabled) yield break;
        obstacle.enabled = false;
        yield return new WaitForSeconds(1f);
        obstacle.enabled = true;
    }

    private void BackToItem(bool destroy = false)
    {
        _data.Agent.enabled = false;
        _data.Grabbable.transform.rotation = _data.OriginalRotation;
        PossessedItemsBehaviour.Instance.SetGrabbableStateServerRpc(_data.Grabbable.NetworkObject, true);

        if (!destroy) return;
        DestroyImmediate(this);
        DestroyImmediate(_data.Agent);
    }
}