using System;
using System.Collections.Generic;
using System.IO;

namespace SocialNetworkApp
{
    // Simple class to hold neighbour + weight
    public class Connection
    {
        public string Neighbour { get; set; }
        public int Weight { get; set; }

        public Connection(string neighbour, int weight)
        {
            Neighbour = neighbour;
            Weight = weight;
        }
    }

    public class SocialNetwork
    {
        // adjacency list: node -> list of connections
        private Dictionary<string, List<Connection>> adjacency_list;

        // flag to say if this graph is treated as weighted or not
        private bool is_weighted;

        public SocialNetwork(bool weighted)
        {
            adjacency_list = new Dictionary<string, List<Connection>>(StringComparer.OrdinalIgnoreCase);
            is_weighted = weighted;
        }

        // add an undirected edge between two nodes
        public void AddEdge(string from, string to, int weight)
        {
            if (!adjacency_list.ContainsKey(from))
            {
                adjacency_list[from] = new List<Connection>();
            }

            if (!adjacency_list.ContainsKey(to))
            {
                adjacency_list[to] = new List<Connection>();
            }

            adjacency_list[from].Add(new Connection(to, weight));
            adjacency_list[to].Add(new Connection(from, weight));   // undirected graph
        }

        public void LoadFromCsv(string file_path)
        {
            if (!File.Exists(file_path))
            {
                Console.WriteLine("File '" + file_path + "' not found.");
                return;
            }

            string[] all_lines = File.ReadAllLines(file_path);

            // assume first line is header
            if (all_lines.Length <= 1)
            {
                Console.WriteLine("No data rows found in the file.");
                return;
            }

            for (int i = 1; i < all_lines.Length; i++)
            {
                string line = all_lines[i].Trim();

                if (string.IsNullOrWhiteSpace(line))
                {
                    // skip blank lines
                    continue;
                }

                string[] parts = line.Split(',');

                if (!is_weighted)
                {
                    // unweighted format node1, node2
                    if (parts.Length < 2) continue;

                    string node1 = parts[0].Trim();
                    string node2 = parts[1].Trim();

                    if (string.IsNullOrWhiteSpace(node1) || string.IsNullOrWhiteSpace(node2))
                    {
                        continue;
                    }

                    AddEdge(node1, node2, 1);
                }
                else
                {
                    // weighted format node1, node2, weight
                    if (parts.Length < 3) continue;

                    string node1 = parts[0].Trim();
                    string node2 = parts[1].Trim();
                    string weight_text = parts[2].Trim();

                    if (string.IsNullOrWhiteSpace(node1) || string.IsNullOrWhiteSpace(node2))
                    {
                        continue;
                    }

                    if (!int.TryParse(weight_text, out int weight))
                    {
                        // bad weight
                        continue;
                    }

                    if (weight <= 0)
                    {
                        // ignore non-positive weights
                        continue;
                    }

                    AddEdge(node1, node2, weight);
                }
            }

            Console.WriteLine("Graph loaded successfully.\n");
        }

        public List<string> GetNodes()
        {
            return new List<string>(adjacency_list.Keys);
        }

        public bool ContainsNode(string node)
        {
            return adjacency_list.ContainsKey(node);
        }

        public double InfluenceScoreUnweighted(string start_node)
        {
            if (is_weighted)
            {
                Console.WriteLine("This network is weighted. Use the weighted function instead.");
                return 0.0;
            }

            if (!adjacency_list.ContainsKey(start_node))
            {
                Console.WriteLine("Start node not found in graph.");
                return 0.0;
            }

            const int INF = int.MaxValue;

            Dictionary<string, int> distance = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Queue<string> queue = new Queue<string>();

            // initialise distances
            foreach (string node in adjacency_list.Keys)
            {
                distance[node] = INF;
            }

            distance[start_node] = 0;
            queue.Enqueue(start_node);

            // BFS
            while (queue.Count > 0)
            {
                string current = queue.Dequeue();

                foreach (Connection conn in adjacency_list[current])
                {
                    string neighbour = conn.Neighbour;

                    if (distance[neighbour] == INF)
                    {
                        distance[neighbour] = distance[current] + 1;
                        queue.Enqueue(neighbour);
                    }
                }
            }

            int total_distance = 0;
            int reachable_count = 0;

            foreach (var pair in distance)
            {
                string node = pair.Key;
                int dist = pair.Value;

                if (node == start_node)
                {
                    continue;
                }

                if (dist != INF)
                {
                    reachable_count = reachable_count + 1;
                    total_distance = total_distance + dist;
                }
            }

            if (reachable_count == 0 || total_distance == 0)
            {
                return 0.0;
            }

            double influence_score = (double)reachable_count / (double)total_distance;
            return influence_score;
        }

        public double InfluenceScoreWeighted(string start_node)
        {
            if (!is_weighted)
            {
                Console.WriteLine("This network is unweighted. Use the unweighted function instead.");
                return 0.0;
            }

            if (!adjacency_list.ContainsKey(start_node))
            {
                Console.WriteLine("Start node not found in graph.");
                return 0.0;
            }

            const int INF = int.MaxValue;

            Dictionary<string, int> distance = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string node in adjacency_list.Keys)
            {
                distance[node] = INF;
            }

            distance[start_node] = 0;

            PriorityQueue<string, int> pq = new PriorityQueue<string, int>();
            pq.Enqueue(start_node, 0);

            // Dijkstra
            while (pq.Count > 0)
            {
                pq.TryDequeue(out string current, out int current_dist);

                if (visited.Contains(current))
                {
                    continue;
                }

                visited.Add(current);

                foreach (Connection conn in adjacency_list[current])
                {
                    string neighbour = conn.Neighbour;
                    int weight = conn.Weight;

                    if (distance[current] == INF)
                    {
                        continue;
                    }

                    int new_dist = distance[current] + weight;

                    if (new_dist < distance[neighbour])
                    {
                        distance[neighbour] = new_dist;
                        pq.Enqueue(neighbour, new_dist);
                    }
                }
            }

            int total_distance = 0;
            int reachable_count = 0;

            foreach (var pair in distance)
            {
                string node = pair.Key;
                int dist = pair.Value;

                if (node == start_node)
                {
                    continue;
                }

                if (dist != INF)
                {
                    reachable_count = reachable_count + 1;
                    total_distance = total_distance + dist;
                }
            }

            if (reachable_count == 0 || total_distance == 0)
            {
                return 0.0;
            }

            double influence_score = (double)reachable_count / (double)total_distance;
            return influence_score;
        }
    }

    class Program
    {
        // currently using absolute paths while testing
        const string UNWEIGHTED_FILE = "C:\\Users\\jakeh\\source\\repos\\Project 3\\Project 3\\unweighted_network.csv";
        const string WEIGHTED_FILE = "C:\\Users\\jakeh\\source\\repos\\Project 3\\Project 3\\weighted_network.csv";

        static void Main(string[] args)
        {
            bool running = true;

            while (running)
            {
                Console.WriteLine("\n Social Network Influence Tool \n");
                Console.WriteLine("1. Load UNWEIGHTED network and calculate influence\n");
                Console.WriteLine("2. Load WEIGHTED network and calculate influence\n");
                Console.WriteLine("0. Exit\n");
                Console.Write("Your choice: ");

                string user_choice = Console.ReadLine()!;

                switch (user_choice)
                {
                    case "1":
                        HandleUnweighted();
                        break;

                    case "2":
                        HandleWeighted();
                        break;

                    case "0":
                        running = false;
                        Console.WriteLine("Exiting...");
                        break;

                    default:
                        Console.WriteLine("Not a valid option.\n");
                        break;
                }
            }
        }

        static void HandleUnweighted()
        {
            SocialNetwork network = new SocialNetwork(weighted: false);

            network.LoadFromCsv(UNWEIGHTED_FILE);

            List<string> nodes = network.GetNodes();

            if (nodes.Count == 0)
            {
                Console.WriteLine("Graph is empty, nothing to calculate.");
                return;
            }

            Console.WriteLine("\nAvailable nodes:");
            foreach (string n in nodes)
            {
                Console.WriteLine(" - " + n);
            }

            Console.Write("\nEnter start node for influence score: ");
            string start = Console.ReadLine()!;

            double score = network.InfluenceScoreUnweighted(start);

            if (score > 0)
            {
                Console.WriteLine("\nInfluence score (unweighted) for " + start + ": " + score.ToString("F2"));
            }
        }

        static void HandleWeighted()
        {
            SocialNetwork network = new SocialNetwork(weighted: true);

            network.LoadFromCsv(WEIGHTED_FILE);

            List<string> nodes = network.GetNodes();

            if (nodes.Count == 0)
            {
                Console.WriteLine("Graph is empty, nothing to calculate.");
                return;
            }

            Console.WriteLine("\nAvailable nodes:");
            foreach (string n in nodes)
            {
                Console.WriteLine(" - " + n);
            }

            Console.Write("\nEnter start node for influence score: ");
            string start = Console.ReadLine()!;

            double score = network.InfluenceScoreWeighted(start);

            if(score > 0)
            {
                Console.WriteLine("\nInfluence score (weighted) for " + start + ": " + score.ToString("F2"));
            }
        }
    }
}
