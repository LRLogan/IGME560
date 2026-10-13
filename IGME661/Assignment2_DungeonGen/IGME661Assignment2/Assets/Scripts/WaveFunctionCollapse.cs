using System;
using System.Collections.Generic;
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
        #region Solver State and Decision History

        private readonly MapTilesData tileData;
        private readonly System.Random random;
        private readonly bool enforceClosedBorders;

        private WFCGrid grid;

        // Every index stored in the wave refers to this list, including rotated options.
        public IReadOnlyList<MapTilesData.TileData> Variants { get; private set; }

        private struct Change
        {
            // One removed possibility, recorded so a failed decision can be undone.
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
            // Number of changes that existed immediately before this decision.
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
            int seed,
            bool enforceClosedBorders = false)
        {
            this.tileData = tileData;
            this.enforceClosedBorders = enforceClosedBorders;
            random = new System.Random(seed);
        }


        #endregion

        #region Generation and Weighted Collapse

        /// <summary>
        /// Collapses cells and propagates constraints, backtracking on contradictions.
        /// The resulting grid describes logical adjacency, not physical prefab positions.
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

            Variants = tileData.CreateVariants();
            changes.Clear();
            decisions.Clear();

            grid = new WFCGrid(
                width,
                height,
                Variants.Count
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
                        return false;
                    }
                }

                while (!Propagate())
                {
                    if (!Backtrack())
                    {
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
                    return location;
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


        /// <summary>
        /// Weighted Shannon entropy: cells with fewer likely choices are resolved first.
        /// </summary>
        private float CalculateEntropy(WFCGrid.Cell cell)
        {
            float totalWeight = 0f;
            float weightedLogSum = 0f;

            foreach (int tileIndex in cell.GetPossibleTiles())
            {
                float weight = Variants[tileIndex].weight;

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


        #endregion

        #region Socket Constraint Propagation

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
        /// Revisits cells in the removal history and propagates their current possibilities.
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
                    {
                        if (enforceClosedBorders &&
                            !ConstrainBoundary(
                                current,
                                direction,
                                queue))
                        {
                            return false;
                        }

                        continue;
                    }

                    WFCGrid.Cell neighbor =
                        grid.GetCell(neighborLocation);

                    bool changed = false;

                    List<int> neighborTiles =
                        new(neighbor.GetPossibleTiles());

                    // A neighbor option survives if at least one current option supports it.
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


        private bool ConstrainBoundary(
            GridLocation location,
            Direction direction,
            Queue<GridLocation> queue)
        {
            WFCGrid.Cell cell = grid.GetCell(location);

            bool changed = false;

            List<int> possibleTiles =
                new(cell.GetPossibleTiles());

            foreach (int tileIndex in possibleTiles)
            {
                MapTilesData.TileData tile =
                    Variants[tileIndex];

                if (!tile.HasOpenSocket(direction))
                    continue;

                RemoveTile(location, tileIndex);
                changed = true;
            }

            if (cell.PossibilityCount == 0)
                return false;

            if (changed)
                queue.Enqueue(location);

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
                Variants[currentTile];

            MapTilesData.TileData neighbor =
                Variants[neighborTile];

            TileSocket currentSocket = current.GetSocket(direction);

            TileSocket neighborSocket =
                neighbor.GetSocket(DirectionUtility.Opposite(direction));

            bool currentOpen =
                currentSocket != null &&
                currentSocket.IsOpen;

            bool neighborOpen =
                neighborSocket != null &&
                neighborSocket.IsOpen;

            if (!currentOpen && !neighborOpen)
                return true;

            if (currentOpen != neighborOpen)
                return false;

            if (!int.TryParse(current.id, out int currentId) ||
                !int.TryParse(neighbor.id, out int neighborId))
            {
                return false;
            }

            // Both sides must permit the connection.
            return currentSocket.Allows(neighborId)
                && neighborSocket.Allows(currentId);
        }


        #endregion

        #region Backtracking and Reversible Changes

        private void RemoveTile(GridLocation location, int tileIndex)
        {
            WFCGrid.Cell cell = grid.GetCell(location);

            if (!cell.Remove(tileIndex))
                return;

            changes.Push(
                new Change(location, tileIndex)
            );
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
            grid.GetCell(location).Restore(tileIndex);
        }

        #endregion

        #region Random Selection Helpers

        // Used for both initial choices and backtracking alternatives.
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
                        Variants[index].weight
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
                        Variants[index].weight
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

        #endregion
    }
}
