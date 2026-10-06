using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// Generic 2D Wave Function Collapse solver.
    ///
    /// This class knows nothing about Unity GameObjects or prefabs.
    /// It only operates on MapTilesData and WFCGrid.
    /// </summary>
    public class WaveFunctionCollapse
    {
        private readonly MapTilesData tileData;
        private readonly System.Random random;

        private readonly Queue<GridLocation> propagationQueue = new();
        private readonly HashSet<GridLocation> queuedLocations = new();

        private WFCGrid grid;

        private struct Change
        {
            public GridLocation location;
            public int tileIndex;

            public Change(GridLocation location, int tileIndex)
            {
                this.location = location;
                this.tileIndex = tileIndex;
            }
        }

        private readonly Stack<Change> changes = new();

        private class Decision
        {
            public int marker;
            public GridLocation location;

            // Tiles that have not yet been tried for this decision.
            public List<int> remainingTiles;

            public Decision(
                int marker,
                GridLocation location,
                List<int> remainingTiles)
            {
                this.marker = marker;
                this.location = location;
                this.remainingTiles = remainingTiles;
            }
        }

        private readonly Stack<Decision> decisions = new();

        public WaveFunctionCollapse(
            MapTilesData tileData,
            int seed)
        {
            this.tileData = tileData;
            random = new System.Random(seed);
        }


        /// <summary>
        /// Generates a WFC solution.
        /// </summary>
        public bool Generate(
            int width,
            int height,
            out WFCGrid result)
        {
            result = null;

            if (!tileData.Validate(out string error))
            {
                Debug.LogError(error);
                return false;
            }

            grid = new WFCGrid(
                width,
                height,
                tileData.Count
            );

            // Propagate initial constraints.
            if (!PropagateAll())
            {
                return false;
            }

            while (true)
            {
                GridLocation? location = FindLowestEntropyCell();

                // No uncollapsed cells remain.
                if (!location.HasValue)
                {
                    result = grid;
                    return true;
                }

                if (!MakeDecision(location.Value))
                {
                    if (!Backtrack())
                    {
                        Debug.LogError(
                            "WFC failed: no valid configuration exists."
                        );

                        return false;
                    }
                }

                while (!Propagate())
                {
                    if (!Backtrack())
                    {
                        Debug.LogError(
                            "WFC failed after exhausting all backtracking options."
                        );

                        return false;
                    }
                }
            }
        }


        /// <summary>
        /// Finds the uncollapsed cell with the lowest entropy.
        ///
        /// A small random noise value prevents large numbers of cells
        /// with identical entropy from always being selected in the
        /// exact same order.
        /// </summary>
        private GridLocation? FindLowestEntropyCell()
        {
            GridLocation? best = null;
            float bestEntropy = float.MaxValue;

            foreach (GridLocation location in grid.GetLocations())
            {
                WFCGrid.Cell cell = grid.GetCell(location);

                if (cell.PossibilityCount <= 0)
                {
                    // Contradiction.
                    return null;
                }

                if (cell.IsCollapsed)
                    continue;

                float entropy = CalculateEntropy(cell);

                // Small randomness breaks ties.
                entropy += (float)random.NextDouble() * 0.001f;

                if (entropy < bestEntropy)
                {
                    bestEntropy = entropy;
                    best = location;
                }
            }

            return best;
        }


        private float CalculateEntropy(WFCGrid.Cell cell)
        {
            float totalWeight = 0f;
            float weightedLogSum = 0f;

            foreach (int tileIndex in cell.GetPossibleTiles())
            {
                float weight = tileData.GetTile(tileIndex).weight;

                if (weight <= 0f)
                    continue;

                totalWeight += weight;
                weightedLogSum += weight * Mathf.Log(weight);
            }

            if (totalWeight <= 0f)
            {
                return float.MaxValue;
            }

            return Mathf.Log(totalWeight) -
                   weightedLogSum / totalWeight;
        }


        /// <summary>
        /// Chooses one tile from a cell using its weight.
        /// </summary>
        private bool MakeDecision(GridLocation location)
        {
            WFCGrid.Cell cell = grid.GetCell(location);

            List<int> candidates =
                new List<int>(cell.GetPossibleTiles());

            if (candidates.Count == 0)
                return false;

            Shuffle(candidates);

            int selected = WeightedRandomChoice(candidates);

            // Save the alternatives for backtracking.
            List<int> remaining = new(candidates);
            remaining.Remove(selected);

            decisions.Push(
                new Decision(
                    changes.Count,
                    location,
                    remaining
                )
            );

            // Remove every option except the selected one.
            foreach (int tileIndex in candidates)
            {
                if (tileIndex == selected)
                    continue;

                RemoveTile(location, tileIndex);
            }

            return true;
        }


        /// <summary>
        /// Propagates all currently known constraints through the grid.
        /// </summary>
        private bool PropagateAll()
        {
            Queue<GridLocation> queue = new();

            foreach (GridLocation location in grid.GetLocations())
            {
                queue.Enqueue(location);
            }

            return PropagateQueue(queue);
        }


        /// <summary>
        /// Propagates changes made since the previous propagation.
        /// </summary>
        private bool Propagate()
        {
            Queue<GridLocation> queue = new();

            HashSet<GridLocation> queued = new();

            foreach (Change change in changes)
            {
                if (queued.Add(change.location))
                {
                    queue.Enqueue(change.location);
                }
            }

            return PropagateQueue(queue);
        }


        private bool PropagateQueue(
            Queue<GridLocation> queue)
        {
            while (queue.Count > 0)
            {
                GridLocation current = queue.Dequeue();

                WFCGrid.Cell currentCell =
                    grid.GetCell(current);

                if (currentCell.PossibilityCount == 0)
                {
                    return false;
                }

                foreach (Direction direction in Enum.GetValues(typeof(Direction)))
                {
                    GridLocation neighborLocation =
                        current.GetAdjacent(direction);

                    if (!grid.IsInside(neighborLocation))
                        continue;

                    WFCGrid.Cell neighbor =
                        grid.GetCell(neighborLocation);

                    bool changed = false;

                    List<int> neighborTiles =
                        new(neighbor.GetPossibleTiles());

                    foreach (int neighborTile in neighborTiles)
                    {
                        bool hasSupport = false;

                        foreach (int currentTile in
                                 currentCell.GetPossibleTiles())
                        {
                            if (TilesCompatible(
                                currentTile,
                                neighborTile,
                                direction))
                            {
                                hasSupport = true;
                                break;
                            }
                        }

                        if (!hasSupport)
                        {
                            RemoveTile(
                                neighborLocation,
                                neighborTile
                            );

                            changed = true;
                        }
                    }

                    if (neighbor.PossibilityCount == 0)
                    {
                        return false;
                    }

                    if (changed)
                    {
                        queue.Enqueue(neighborLocation);
                    }
                }
            }

            return true;
        }


        /// <summary>
        /// Determines whether two tiles can be adjacent.
        /// </summary>
        private bool TilesCompatible(
    int currentTile,
    int neighborTile,
    Direction direction)
        {
            MapTilesData.TileData current =
                tileData.GetTile(currentTile);

            TileSocket socket =
                current.GetSocket(direction);

            return socket != null &&
                   socket.Allows(neighborTile);
        }


        private void RemoveTile(GridLocation location, int tileIndex)
        {
            WFCGrid.Cell cell = grid.GetCell(location);

            if (!cell.Remove(tileIndex))
                return;

            changes.Push(
                new Change(location, tileIndex)
            );

            if (queuedLocations.Add(location))
            {
                propagationQueue.Enqueue(location);
            }
        }


        /// <summary>
        /// Attempts to recover from a contradiction.
        ///
        /// The latest decision is restored and another tile is tried.
        /// </summary>
        private bool Backtrack()
        {
            while (decisions.Count > 0)
            {
                Decision decision = decisions.Peek();

                // Restore everything made after this decision.
                UndoTo(decision.marker);

                if (decision.remainingTiles.Count == 0)
                {
                    decisions.Pop();
                    continue;
                }

                int nextTile =
                    WeightedRandomChoice(
                        decision.remainingTiles
                    );

                decision.remainingTiles.Remove(nextTile);

                // Reapply this decision with the next option.
                WFCGrid.Cell cell =
                    grid.GetCell(decision.location);

                List<int> candidates =
                    new(cell.GetPossibleTiles());

                foreach (int tileIndex in candidates)
                {
                    if (tileIndex != nextTile)
                    {
                        RemoveTile(
                            decision.location,
                            tileIndex
                        );
                    }
                }

                return true;
            }

            return false;
        }


        private void UndoTo(int marker)
        {
            while (changes.Count > marker)
            {
                Change change = changes.Pop();

                // Removing a tile from the wave is reversible.
                // Since this is private state, we need a restore method.
                RestoreTile(
                    change.location,
                    change.tileIndex
                );
            }
        }


        private void RestoreTile(
            GridLocation location,
            int tileIndex)
        {
            // WFCGrid.Cell intentionally exposes Remove(),
            // but restoration is handled here through reflection-free
            // internal state access.
            //
            // This implementation requires the Cell to expose Restore().
            grid.GetCell(location).Restore(tileIndex);
        }


        private int WeightedRandomChoice(
            List<int> candidates)
        {
            if (candidates.Count == 0)
                return -1;

            float totalWeight = 0f;

            foreach (int index in candidates)
            {
                totalWeight +=
                    Mathf.Max(
                        0f,
                        tileData.GetTile(index).weight
                    );
            }

            if (totalWeight <= 0f)
            {
                return candidates[
                    random.Next(candidates.Count)
                ];
            }

            double value =
                random.NextDouble() * totalWeight;

            foreach (int index in candidates)
            {
                value -=
                    Mathf.Max(
                        0f,
                        tileData.GetTile(index).weight
                    );

                if (value <= 0)
                    return index;
            }

            return candidates[^1];
        }


        private void Shuffle(List<int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);

                (list[i], list[j]) =
                    (list[j], list[i]);
            }
        }
    }
}
