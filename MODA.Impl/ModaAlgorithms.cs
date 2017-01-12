﻿using QuickGraph;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MODA.Impl
{
    public partial class ModaAlgorithms
    {
        /// <summary>
        /// Frequency value, above which we can comsider the subgraph a "frequent subgraph"
        /// </summary>
        public static int Threshold { get; set; }
        /// <summary>
        /// If true, it means we only care about how many mappings are found for each subgraph, not info about the mappings themselves.
        /// </summary>
        public static bool GetOnlyMappingCounts { get; set; }
        /// <summary>
        /// If true, the program will use my modified Grochow's algorithm (Algo 2)
        /// </summary>
        public static bool UseModifiedGrochow { get; set; }
        /// <summary>
        /// The query graph to be searched for. If not available, we use expansion trees (MODA). Otherwise, we use Grochow's (Algo 2)
        /// </summary>
        public static QueryGraph QueryGraph { get; set; }
        
        #region Useful mainly for the Algorithm 2 versions

        /// <summary>
        /// Given a set of nodes(the Key), we find the subgraph in the input graph G that has those nodes.
        /// </summary>
        private static Dictionary<string[], UndirectedGraph<string, Edge<string>>> InputSubgraphs;

        internal static Dictionary<string, List<string>> G_NodeNeighbours;
        internal static Dictionary<string, List<string>> H_NodeNeighbours;

        private static Dictionary<string[], HashSet<string>> NeighboursOfRange;
        private static Dictionary<string[], string> MostConstrainedNeighbours;

        /// <summary>
        /// Algorithm taken from Grochow and Kellis. This is failing at the moment
        /// </summary>
        /// <param name="partialMap">f; Map is represented as a dictionary, with the Key as h and the Value as g</param>
        /// <param name="queryGraph">G</param>
        /// <param name="inputGraph">H</param>
        /// <returns>List of isomorphisms. Remember, Key is h, Value is g</returns>
        private static List<Mapping> IsomorphicExtension(Dictionary<string, string> partialMap, UndirectedGraph<string, Edge<string>> queryGraph
            , UndirectedGraph<string, Edge<string>> inputGraph)
        {
            if (partialMap.Count == queryGraph.VertexCount)
            {
                #region Return base case
                var map = new Mapping(partialMap);
                foreach (var qEdge in queryGraph.Edges)
                {
                    map.MapOnInputSubGraph.AddVerticesAndEdge(new Edge<string>(partialMap[qEdge.Source], partialMap[qEdge.Target]));
                }

                string[] inputSubgraphKey = partialMap.Values.ToArray();
                var exists = InputSubgraphs.ContainsKey(inputSubgraphKey);
                if (!exists)
                {
                    var newInputSubgraph = new UndirectedGraph<string, Edge<string>>(false);
                    for (int i = 0; i < inputSubgraphKey.Length; i++)
                    {
                        for (int j = (i + 1); j < inputSubgraphKey.Length; j++)
                        {
                            Edge<string> edge;
                            if (inputGraph.TryGetEdge(inputSubgraphKey[i], inputSubgraphKey[j], out edge))
                            {
                                newInputSubgraph.AddVerticesAndEdge(edge);
                            }
                        }
                    }
                    InputSubgraphs[inputSubgraphKey] = newInputSubgraph;

                }
                map.InputSubGraph = InputSubgraphs[inputSubgraphKey];

                inputSubgraphKey = null;
                return new List<Mapping> { map };
                #endregion
            }

            //Remember: f(h) = g, so h is Domain and g is Range.
            //  In other words, Key is h and Value is g in the dictionary

            // get m, most constrained neighbor
            string m = GetMostConstrainedNeighbour(partialMap.Keys.ToArray(), queryGraph);
            if (string.IsNullOrWhiteSpace(m)) return new List<Mapping>();

            var listOfIsomorphisms = new List<Mapping>();

            var neighbourRange = ChooseNeighboursOfRange(partialMap.Values.ToArray(), inputGraph);
            foreach (var n in neighbourRange) //foreach neighbour n of f(D)
            {
                if (IsNeighbourIncompatible(inputGraph, queryGraph, n, m, partialMap))
                {
                    continue;
                }
                //It's not; so, let f' = f on D, and f'(m) = n.
                var newPartialMap = new Dictionary<string, string>(partialMap.Count + 1);
                foreach (var item in partialMap)
                {
                    newPartialMap.Add(item.Key, item.Value);
                }
                newPartialMap[m] = n;

                //Find all isomorphic extensions of f'.
                var subList = IsomorphicExtension(newPartialMap, queryGraph, inputGraph);
                if (listOfIsomorphisms.Count == 0)
                {
                    listOfIsomorphisms.AddRange(subList);
                }
                else
                {
                    subList.ForEach(item =>
                    {
                        if (new HashSet<string>(item.Function.Values).Count == item.Function.Count)
                        {
                            var existing = listOfIsomorphisms.Find(x => x.Equals(item));

                            if (existing == null)
                            {
                                listOfIsomorphisms.Add(item);
                            }
                            existing = null;
                        }
                    });
                }
                newPartialMap = null;
            }
            return listOfIsomorphisms;
        }

        /// <summary>
        /// If there is a neighbor d ∈ D of m such that n is NOT neighbors with f(d),
        /// or if there is a NON-neighbor d ∈ D of m such that n is neighbors with f(d) 
        /// [or if assigning f(m) = n would violate a symmetry-breaking condition in C(h)]
        /// then contiue with the next n
        /// </summary>
        /// <param name="queryGraph">H</param>
        /// <param name="inputGraph">G</param>
        /// <param name="n">g_node, pass in 'neighbour'; n in Grochow</param>
        /// <param name="m">h_node; the most constrained neighbor of any d ∈ D</param>
        /// <param name="domain">domain_in_H</param>
        /// <param name="partialMap">function</param>
        /// <returns></returns>
        private static bool IsNeighbourIncompatible(UndirectedGraph<string, Edge<string>> inputGraph, UndirectedGraph<string, Edge<string>> queryGraph,
            string n, string m, Dictionary<string, string> partialMap)
        {
            //  RECALL: m is for Domain, the Key => the query graph

            //A: If there is a neighbor d ∈ D of m such that n is NOT neighbors with f(d)...
            var neighboursOfN = inputGraph.GetNeighbors(n);
            var neighborsOfM = queryGraph.GetNeighbors(m, false);
            for (int i = 0; i < neighborsOfM.Count; i++)
            {
                if (!partialMap.ContainsKey(neighborsOfM[i]))
                {
                    neighboursOfN = null;
                    return false;
                }
                if (!neighboursOfN.Contains(partialMap[neighborsOfM[i]]))
                {
                    neighboursOfN = null;
                    return true;
                }
            }

            ////B: ...or if there is a NON-neighbor d ∈ D of m such that n is neighbors with f(d) 
            //var nonNeighborsOfM = queryGraph.GetNonNeighbors(m, neighborsOfM);
            //foreach (var d in nonNeighborsOfM)
            //{
            //    if (!partialMap.ContainsKey(d)) return false;

            //    if (neighboursOfN.Contains(partialMap[d]))
            //    {
            //        return false;
            //    }
            //}

            neighborsOfM = null;
            neighboursOfN = null;
            return false;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static HashSet<string> ChooseNeighboursOfRange(string[] used_range, UndirectedGraph<string, Edge<string>> inputGraph)
        {
            var exists = NeighboursOfRange.ContainsKey(used_range);
            if (!exists)
            {
                var result = new HashSet<string>();
                for (int i = used_range.Length - 1; i >= 0; i--)
                {
                    var local = inputGraph.GetNeighbors(used_range[i]);
                    if (local.Count == 0)
                    {
                        local = null;
                        continue;
                    }
                    /* You'd wonder why I didn't just do this:
                     * for (int j = 0; j < local.Count; j++)
                        {
                            if (!used_range.Contains(local[j]))
                            {
                                result.Add(local[j]);
                            }
                        }
                     * instead of the creepy thing below.
                     * Well, it turns out that, for whatever reason only the compiler knows, the above code
                     * makes the program incredibly slow. Code that runs in 15secs suddenly started taking over 3hours. 
                     * */
                    int counter = 0;
                    for (int j = 0; j < local.Count + counter; j++)
                    {
                        if (used_range.Contains(local[j - counter]))
                        {
                            try
                            {
                                local.Remove(local[j - counter]);
                                counter++;
                            }
                            catch { }
                            if (local.Count == 0)
                            {
                                break;
                            }
                        }
                    }
                    foreach (var item in local)
                    {
                        result.Add(item);
                    }

                    local = null;
                }
                NeighboursOfRange[used_range] = result;

                result = null;
            }

            return NeighboursOfRange[used_range];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="domain">Domain, D, of fumction f</param>
        /// <param name="queryGraph">H</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetMostConstrainedNeighbour(string[] domain, UndirectedGraph<string, Edge<string>> queryGraph)
        {
            /*
             * As is standard in backtracking searches, the algorithm uses the most constrained neighbor
             * to eliminate maps that cannot be isomorphisms: that is, the neighbor of the already-mapped 
             * nodes which is likely to have the fewest possible nodes it can be mapped to. First we select 
             * the nodes with the most already-mapped neighbors, and amongst those we select the nodes with 
             * the highest degree and largest neighbor degree sequence.
             * */
            var exists = MostConstrainedNeighbours.ContainsKey(domain);
            if (!exists)
            {
                var result = new List<string>();
                for (int i = domain.Length - 1; i >= 0; i--)
                {
                    var local = queryGraph.GetNeighbors(domain[i], false);
                    if (local.Count == 0)
                    {
                        local = null;
                        continue;
                    }
                    /* You'd wonder why I didn't just do this:
                     * for (int j = 0; j < local.Count; j++)
                        {
                            if (!domain.Contains(local[j]))
                            {
                                result.Add(local[j]);
                            }
                        }
                     * instead of the creepy thing below.
                     * Well, it turns out that, for whatever reason only the compiler knows, the above code
                     * makes the program incredibly slow. Code that runs in 15secs suddenly started taking over 3hours. 
                     * */
                    int counter = 0;
                    for (int j = 0; j < local.Count + counter; j++)
                    {
                        if (domain.Contains(local[j - counter]))
                        {
                            try
                            {
                                local.Remove(local[j - counter]);
                                counter++;
                            }
                            catch { }
                            if (local.Count == 0)
                            {
                                break;
                            }
                        }
                    }
                    foreach (var item in local)
                    {
                        result.Add(item);
                    }

                    local = null;
                }
                if (result.Count == 0)
                {
                    MostConstrainedNeighbours[domain] = "";
                }
                else
                {
                    MostConstrainedNeighbours[domain] = result[0];
                }
                result = null;
            }

            return MostConstrainedNeighbours[domain];
        }

        /// <summary>
        /// We say that <paramref name="node_G"/> (g) of <paramref name="inputGraph"/> (G) can support <paramref name="node_H"/> (h) of <paramref name="queryGraph"/> (H)
        /// if we cannot rule out a subgraph isomorphism from H into G which maps h to g based on the degrees of h and g, and the degree of their neighbours
        /// </summary>
        /// <param name="queryGraph">H</param>
        /// <param name="node_H">h</param>
        /// <param name="inputGraph">G</param>
        /// <param name="node_G">g</param>
        /// <returns></returns>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CanSupport(UndirectedGraph<string, Edge<string>> queryGraph, string node_H, UndirectedGraph<string, Edge<string>> inputGraph, string node_G)
        {
            // 1. Based on their degrees
            if (inputGraph.AdjacentDegree(node_G) < queryGraph.AdjacentDegree(node_H)) return false;

            //So, deg(g) >= deg(h).
            //2. Based on the degree of their neighbors
            var gNeighbors = inputGraph.GetNeighbors(node_G);
            var hNeighbors = queryGraph.GetNeighbors(node_H);
            for (int i = hNeighbors.Count - 1; i >= 0; i--)
            {
                for (int j = gNeighbors.Count - 1; j >= 0; j--)
                {
                    if (inputGraph.AdjacentDegree(gNeighbors[j]) >= queryGraph.AdjacentDegree(hNeighbors[i]))
                    {
                        gNeighbors = null;
                        hNeighbors = null;
                        return true;
                    }
                }
            }
            gNeighbors = null;
            hNeighbors = null;
            return false;
        }

        #endregion
    }
}