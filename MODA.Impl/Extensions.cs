﻿using QuickGraph;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MODA.Impl
{
    public static class Extensions
    {
        public static bool IsIsomorphicWith(this Mapping thisMapping, Mapping otherMapping, UndirectedGraph<string, Edge<string>> inputSubgraph)
        {
            //Test 0 - basic object test
            if (otherMapping == null) return false;

            //Test 1 - Vertices - sameness and count
            var areEqual = (new HashSet<string>(thisMapping.Function.Values).SetEquals(otherMapping.Function.Values)
                && new HashSet<string>(thisMapping.Function.Keys).SetEquals(otherMapping.Function.Keys));
            if (areEqual == false)
            {
                return false;
            }

            //Test 2 - Edge count
            areEqual = thisMapping.MapOnInputSubGraph.EdgeCount == otherMapping.MapOnInputSubGraph.EdgeCount;
            if (areEqual == false)
            {
                return false;
            }

            //Test 3 - Node degrees.
            var any = thisMapping.MapOnInputSubGraph.Vertices.ToList().Find(node =>
                    thisMapping.MapOnInputSubGraph.AdjacentDegree(node) != otherMapping.MapOnInputSubGraph.AdjacentDegree(node));
            //var any = thisMapping.MapOnInputSubGraph.Vertices.FirstOrDefault(node =>
            //        thisMapping.MapOnInputSubGraph.AdjacentDegree(node) != otherMapping.MapOnInputSubGraph.AdjacentDegree(node));
            if (any != null)
            {
                //if input sub-graph is complete
                if (inputSubgraph.IsComplete())
                {
                    // Then the subgraphs is likely isomorphic, due to symmetry
                    return true;
                }
                return false;
            }
            foreach (var node in thisMapping.MapOnInputSubGraph.Vertices)
            {
                if (thisMapping.MapOnInputSubGraph.AdjacentDegree(node) != otherMapping.MapOnInputSubGraph.AdjacentDegree(node))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// A Complete graph of n nodes has n(n-1)/2 edges
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns>
        public static bool IsComplete(this UndirectedGraph<string, Edge<string>> graph)
        {
            return graph.EdgeCount == ((graph.VertexCount * (graph.VertexCount - 1)) / 2);
        }

        public static List<string> GetNeighbors(this UndirectedGraph<string, Edge<string>> graph, string vertex, bool isG)
        {
            if (string.IsNullOrWhiteSpace(vertex)) return new List<string>();
            //List<string> neighbors;
            HashSet<string> set;
            if (isG)
            {
                var adjEdges = graph.AdjacentEdges(vertex);
                set = new HashSet<string>(); // (adjEdges.Select(x => x.Source).Union(adjEdges.Select(x => x.Target)));
                foreach (var edge in adjEdges)
                {
                    set.Add(edge.Source);
                    set.Add(edge.Target);
                }
                set.Remove(vertex);
                //neighbors = set.ToList();
                adjEdges = null;
            }
            else
            {
                var adjEdges = graph.AdjacentEdges(vertex);
                set = new HashSet<string>(); // (adjEdges.Select(x => x.Source).Union(adjEdges.Select(x => x.Target)));
                foreach (var edge in adjEdges)
                {
                    set.Add(edge.Source);
                    set.Add(edge.Target);
                }
                set.Remove(vertex);
                //neighbors = set.ToList();
                adjEdges = null;
            }

            return set.ToList();
        }

        public static string AsString(this UndirectedGraph<string, Edge<string>> graph)
        {
            if (graph.IsEdgesEmpty) return "";
            var sb = new StringBuilder("Graph-Edges_");
            foreach (var edge in graph.Edges)
            {
                sb.AppendFormat("[{0}],", edge);
            }
            //sb.AppendLine();
            return sb.ToString().TrimEnd();
        }

        public static UndirectedGraph<string, Edge<string>> Clone(this UndirectedGraph<string, Edge<string>> graph)
        {
            var inputGraphClone = new UndirectedGraph<string, Edge<string>>();
            inputGraphClone.AddVerticesAndEdgeRange(graph.Edges);
            Debug.Assert(inputGraphClone.EdgeCount == graph.EdgeCount && inputGraphClone.VertexCount == graph.VertexCount);
            return inputGraphClone;
        }

        /// <summary>
        /// NB: The degree sequence of an undirected graph is the non-increasing sequence of its vertex degrees;
        /// </summary>
        /// <typeparam name="string"></typeparam>
        /// <param name="graph"></param>
        /// <param name="count">The expected number of items to return.</param>
        /// <returns></returns>
        public static string[] GetDegreeSequence(this UndirectedGraph<string, Edge<string>> graph, int count)
        {
            if (graph.IsVerticesEmpty) return new string[0];

            var vertices = graph.Vertices.Take(count).ToArray();
            var tempList = new Dictionary<string, int>(vertices.Length);

            foreach (var node in vertices)
            {
                tempList.Add(node, graph.AdjacentDegree(node));
            }

            var listToReturn = new List<string>(vertices.Length);
            foreach (var item in tempList.OrderByDescending(x => x.Value))
            {
                listToReturn.Add(item.Key);
            }

            vertices = null;
            tempList = null;
            return listToReturn.ToArray();
        }

        /// <summary>
        /// Converts a sequence of edges into a query graph
        /// </summary>
        /// <param name="edges"></param>
        /// <returns></returns>
        public static QueryGraph ToQueryGraph(this IEnumerable<Edge<string>> edges, string graphLabel = "")
        {
            var g = new QueryGraph
            {
                Label = graphLabel
            };
            g.AddVerticesAndEdgeRange(edges);
            return g;
        }
    }
}
