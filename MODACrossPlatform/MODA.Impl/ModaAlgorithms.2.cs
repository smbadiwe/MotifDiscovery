﻿using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MODA.Impl
{
    public partial class ModaAlgorithms
    {
        /// <summary>
        /// Mapping module; aka FindSubgraphInstances in Grochow & Kellis
        /// </summary>
        /// <param name="queryGraph">H</param>
        /// <param name="inputGraph">G</param>
        /// <param name="numberOfSamples">To be decided. If not set, we use the <paramref name="inputGraph"/> size</param>
        private static List<Mapping> Algorithm2(QueryGraph queryGraph, UndirectedGraph<string, Edge<string>> inputGraph, int numberOfSamples = -1)
        {
            //var timer = System.Diagnostics.Stopwatch.StartNew();
            if (numberOfSamples <= 0) numberOfSamples = inputGraph.VertexCount / 3; // VertexCountDividend;

            // Do we need this clone? Can't we just remove the node directly from the graph? 
            // We do need it.
            var inputGraphClone = inputGraph.Clone();

            var comparer = new MappingNodesComparer();
            InputSubgraphs = new Dictionary<string[], UndirectedGraph<string, Edge<string>>>(comparer);
            MostConstrainedNeighbours = new Dictionary<string[], string>(comparer);
            NeighboursOfRange = new Dictionary<string[], HashSet<string>>(comparer);
            comparer = null;

            G_NodeNeighbours = new Dictionary<string, List<string>>();
            H_NodeNeighbours = new Dictionary<string, List<string>>();

            var theMappings = new Dictionary<string, List<Mapping>>();
            var queryGraphVertices = queryGraph.Vertices.ToList();
            foreach (var g in inputGraph.GetDegreeSequence(numberOfSamples))
            {
                foreach (var h in queryGraphVertices)
                {
                    if (CanSupport(queryGraph, h, inputGraphClone, g))
                    {
                        #region Can Support
                        //var sw = System.Diagnostics.Stopwatch.StartNew();
                        //Remember: f(h) = g, so h is Domain and g is Range
                        var function = new Dictionary<string, string>(1) { { h, g } }; //function, f
                        var mappings = IsomorphicExtension(function, queryGraph, inputGraphClone);
                        if (mappings.Count == 0) continue;

                        //sw.Stop();
                        var logGist = new StringBuilder();
                        logGist.AppendFormat("Maps gotten from IsomorphicExtension.\th = {0}. g = {1}\t", h, g);
                        //logGist.AppendFormat("Maps gotten from IsomorphicExtension.\tTook:\t{0:N}s.\th = {1}. g = {2}", sw.Elapsed.ToString(), h, g);
                        //sw.Restart();

                        foreach (Mapping mapping in mappings)
                        {
                            List<Mapping> mappingsToSearch; //Recall: f(h) = g
                            var g_key = mapping.Function.Last().Value;
                            if (theMappings.TryGetValue(g_key, out mappingsToSearch))
                            {
                                var existing = mappingsToSearch.Find(x => x.Equals(mapping));

                                if (existing == null)
                                {
                                    theMappings[g_key].Add(mapping);
                                }
                            }
                            else
                            {
                                theMappings[g_key] = new List<Mapping> { mapping };
                            }
                            mappingsToSearch = null;
                        }
                        
                        function = null;
                        //sw.Stop();
                        //logGist.AppendFormat("Map: {0}.\tTime to set:\t{1:N}s.\th = {2}. g = {3}\n", mappings.Count, sw.Elapsed.ToString(), h, g);
                        //sw = null;
                        logGist.AppendFormat("Mappings Count: {0}.\n", mappings.Count);
                        logGist.AppendFormat("*****************************************\n");
                        Console.WriteLine(logGist);
                        mappings = null;
                        logGist = null;
                        #endregion
                    }
                }

                //Remove g
                inputGraphClone.RemoveVertex(g);
            }

            var toReturn = new List<Mapping>();
            foreach (var mapping in theMappings)
            {
                toReturn.AddRange(mapping.Value);
            }
            Console.WriteLine("Algorithm 2: All tasks completed. Number of mappings found: {0}.\n", toReturn.Count);

            //timer.Stop();
            //Console.WriteLine("Algorithm 2: All tasks completed. Number of mappings found: {0}.\nTotal time taken: {1}", toReturn.Count, timer.Elapsed.ToString());
            //timer = null;
            queryGraphVertices = null;
            G_NodeNeighbours = null;
            H_NodeNeighbours = null;
            inputGraphClone = null;
            return toReturn;
        }
    }
}