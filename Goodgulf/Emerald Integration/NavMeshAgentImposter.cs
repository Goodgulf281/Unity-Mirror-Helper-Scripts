using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Pathfinding;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

namespace Pathfinding
{

    /* This class acts as an intermediate between the Emerald AI and AStarpathfinding code.
     * Since Emerald AI is built on Unity Navmesh I created an imposter which appears to be Unity Navmesh but uses
     * AStarpathfinding calls instead. So basically it translated all properties and methods which Emerald uses
     * from Unity NavMesh, see https://docs.unity3d.com/2022.3/Documentation/ScriptReference/AI.NavMeshAgent.html
     *
     * I also added some additional debug code which can be enabled optionally using the SetDebugLevel method.
     */

    public class NavMeshAgentImposter : AIPath
    {
        private bool _autoBraking;
        private bool _isGridGraph = true;
        private int _debugLevel = 0;

        public int areaMask; // Todo: assign some meaningful value

        public int debugLevel
        {
            get => _debugLevel;
        }

        // https://docs.unity3d.com/2022.3/Documentation/ScriptReference/AI.NavMeshAgent-stoppingDistance.html
        // This has >40 usages in the Emerald code so it would require lots of additional code changes without this "imposter" code.
        public float stoppingDistance
        {
            get => endReachedDistance;
            set
            {
                if (_debugLevel > 1)
                {
                    Debug.Log($"NavMeshAgentImposter.stoppingDistance={value}");
                }
                else if (_debugLevel > 0)
                {
                    if (value < endReachedDistance)
                        Debug.Log($"NavMeshAgentImposter.stoppingDistance reduced to={value}");
                }

                endReachedDistance = value;
            }
        }

        // https://docs.unity3d.com/2022.3/Documentation/ScriptReference/AI.NavMeshAgent-speed.html
        public float speed
        {
            get => maxSpeed;
            set
            {
                if (_debugLevel > 1)
                {
                    Debug.Log($"NavMeshAgentImposter.speed={value}");
                }
                else if (_debugLevel > 0)
                {
                    if (value < speed)
                        Debug.Log($"NavMeshAgentImposter.speed reduced to={value}");
                }

                maxSpeed = value;
            }
        }

        // https://docs.unity3d.com/2022.3/Documentation/ScriptReference/AI.NavMeshAgent-autoBraking.html
        public bool autoBraking
        {
            get => _autoBraking;
            set
            {
                if (_debugLevel > 1)
                {
                    Debug.Log($"NavMeshAgentImposter.autoBraking={value}");
                }
                else if (_debugLevel > 0)
                {
                    if (value != _autoBraking)
                        Debug.Log($"NavMeshAgentImposter.autoBraking changed to={value}");
                }

                _autoBraking = value;
                if (value)
                {
                    whenCloseToDestination = CloseToDestinationMode.Stop;
                }
                else
                {
                    whenCloseToDestination = CloseToDestinationMode.ContinueToExactDestination;
                }
            }
        }


        public bool isOnNavMesh
        {
            get => IsOnNavMesh();
        }

        // https://docs.unity3d.com/2022.3/Documentation/ScriptReference/AI.NavMeshAgent-isOnNavMesh.html
        // I used the code from this forum post:
        // https://forum.arongranberg.com/t/how-to-check-the-destination-can-be-reached/5554
        // Since I only tested with the AStarpathfinding grid mesh this may require additional work.
        public bool IsOnNavMesh()
        {
            if (_debugLevel > 1)
                Debug.Log($"NavMeshAgentImposter.IsOnNavMesh() called with isGridGraph={_isGridGraph}");

            if (_isGridGraph)
            {
                return AstarPath.active.GetNearest(transform.position, NNConstraint.None).node.Walkable;
            }
            else
            {
                var node = AstarPath.active.data.recastGraph.PointOnNavmesh(transform.position, NNConstraint.Walkable);
                if (node != null)
                {
                    return true;
                }

                return false;
            }
        }

        // This is a helper function so IsOnNaveMesh() knows what kind of graph we are using. I'm now setting this
        // in Emerald's EmeraldMovement.SetupNavMeshAgent() but that's currently a shortcut.       
        public void SetGraphMode(bool isGridGraph = true)
        {
            if (_debugLevel > 0)
                Debug.Log($"NavMeshAgentImposter.SetGraphMode(): set GraphMode to {isGridGraph}");

            _isGridGraph = isGridGraph;
        }

        // https://docs.unity3d.com/2022.3/Documentation/ScriptReference/AI.NavMeshAgent.ResetPath.html
        public void ResetPath()
        {
            //canSearch = false;
            SetPath(null);
            if (_debugLevel > 0)
                Debug.Log($"<color=yellow>NavMeshAgentImposter.ResetPath() called</color>");
        }

        // https://docs.unity3d.com/2022.3/Documentation/ScriptReference/AI.NavMeshAgent.SetDestination.html
        public bool SetDestination(Vector3 target)
        {
            canSearch = true;
            destination = target;

            if (_debugLevel > 0)
                Debug.Log($"<color=yellow>NavMeshAgentImposter.SetDestination(): target = {target}</color>");

            return true;
        }

        public void SetDebugLevel(int level)
        {
            Debug.Log($"NavMeshAgentImposter.SetDebugLevel(): set debug level to {level}");
            _debugLevel = level;
        }

        // https://docs.unity3d.com/2022.3/Documentation/ScriptReference/AI.NavMeshAgent.Warp.html
        // Note: I don't think this is tested in any of the demo scripts.
        public bool Warp(Vector3 newPosition)
        {
            if (_debugLevel > 0)
                Debug.Log($"<color=yellow>NavMeshAgentImposter.Warp(): target = {newPosition}</color>");

            Teleport(newPosition);
            return true;
        }

        // Initialize the mask values. Todo: this is code should be more flexible since it now uses default layer names. 
        public void Initialize()
        {
            if (_debugLevel > 0)
                Debug.Log($"<color=yellow>NavMeshAgentImposter.Initialize(): setting masks</color>");

            // Change these masks is you use a layer for your ground/navigation grid other than Default
            areaMask = LayerMask.GetMask("Default");
            groundMask = LayerMask.GetMask("Default");
        }

        public void SetMasks(int _areaMask, int _groundMask)
        {
            if (_debugLevel > 0)
                Debug.Log(
                    $"<color=yellow>NavMeshAgentImposter.SetMasks(): areaMask={_areaMask}, groundMask{_groundMask}</color>");

            areaMask = _areaMask;
            groundMask = _groundMask;
        }

        // This code simplifies code changes to Emerald Movement. It is only used 3 times in the emerald code.
        public void SetDestinationWithWaypoint(Vector3 waypoint)
        {
            if (_debugLevel > 0)
                Debug.Log(
                    $"<color=yellow>NavMeshAgentImposter.SetDestinationWithWaypoint(): waypoint = {waypoint}</color>");

            // Add a small offset to the Y coord. This is needed by Emerald Movement to ensure it picks the next waypoint.
            // If the destination matches the exact waypoint coords the agent can get stuck at a waypoint.
            Vector3 _destination = new Vector3(waypoint.x, waypoint.y + 0.1f, waypoint.z);
            destination = _destination;
        }
    }

}