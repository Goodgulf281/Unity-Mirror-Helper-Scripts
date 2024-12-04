using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Pathfinding;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

namespace Pathfinding
{



    public class NavMeshAgentImposter : AIPath
    {
        private bool _autoBraking;

//  private bool _isOnNavMesh = true;
        private bool _isGridGraph = true;
        private int _debugLevel = 0;

        public int areaMask; // Todo: assign some meaningful value

        public int debugLevel
        {
            get => _debugLevel;
        }

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

        /* Not tested, comment out this block if you derive NaMeshAgentImposter from RichAI instead of AIPath
        
        public float maxAcceleration
        {
            get => acceleration;
            set
            {
                if (_debugLevel > 1)
                {
                    Debug.Log($"NavMeshAgentImposter.acceleration={value}");
                }
                else if (_debugLevel > 0)
                {
                    if (value < acceleration)
                        Debug.Log($"NavMeshAgentImposter.acceleration reduced to={value}");
                    else
                        Debug.Log($"NavMeshAgentImposter.acceleration increased to={value}");
                }

                acceleration = value;
            }
        }
        */

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

        // https://forum.arongranberg.com/t/unity-navmesh-to-a-conversion/7888

        public bool isOnOffMeshLink
        {
            get => IsOnOffMeshLink();
        }


        public bool IsOnOffMeshLink()
        {
            if (_isGridGraph)
            {
                return false;
            }
            else
            {
                // This can only be used if you derive the imposter from RichAI instead of AIPath:
                // return traversingOffMeshLink;
                return false;
            }    
        }
        
        public bool isOnNavMesh
        {
            get => IsOnNavMesh();
        }

        // https://forum.arongranberg.com/t/how-to-check-the-destination-can-be-reached/5554

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


        public void SetGraphMode(bool isGridGraph = true)
        {
            if (_debugLevel > 0)
                Debug.Log($"NavMeshAgentImposter.SetGraphMode(): set GraphMode to {isGridGraph}");

            _isGridGraph = isGridGraph;
        }

        public void ResetPath()
        {
            //canSearch = false;
            SetPath(null);
            if (_debugLevel > 0)
                Debug.Log($"<color=yellow>NavMeshAgentImposter.ResetPath() called</color>");
        }

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

        public bool Warp(Vector3 newPosition)
        {
            if (_debugLevel > 0)
                Debug.Log($"<color=yellow>NavMeshAgentImposter.Warp(): target = {newPosition}</color>");

            Teleport(newPosition);
            return true;
        }

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

        public void SetDestinationWithWaypoint(Vector3 waypoint)
        {
            if (_debugLevel > 0)
                Debug.Log(
                    $"<color=yellow>NavMeshAgentImposter.SetDestinationWithWaypoint(): waypoint = {waypoint}</color>");

            // Add a small offset to the Y coord.
            Vector3 _destination = new Vector3(waypoint.x, waypoint.y + 0.1f, waypoint.z);
            destination = _destination;
        }
    }

}