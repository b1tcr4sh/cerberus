namespace Cerberus {
    public class DotEnv {
        private Dictionary<string, string> variables = new Dictionary<string, string>();
        public DotEnv() {
            if (!File.Exists("./.env")) {
                return;
            }

            foreach (string line in File.ReadAllLines("./.env")) {
                string[] split = line.Split("=");

                if (split.Length > 2) throw new Exception("Environment vars formatted incorrectly.. ?");

                variables.Add(split[0], split[1]);
            }
        }

        public string Get(string key) {
            string value;

            if (!variables.TryGetValue(key, out value)) {
                throw new Exception(String.Format("variable with key {0} not found!", key));
            }

            return value;
        }
    }
}