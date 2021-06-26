using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace PA_LR3
{
    class Program
    {
        static void Main(string[] args)
        {
            CitiesData citiesData = new CitiesData(300);
            // Console.WriteLine(citiesData.ToString());
            Console.WriteLine("Best path = " + citiesData.FirstPathSum());
            int totalNumberBees = 100;
            Math.Round(totalNumberBees * 0.64, 0, MidpointRounding.AwayFromZero);
            int numberInactive = Convert.ToInt32(Math.Round(totalNumberBees * 0.64, 0, MidpointRounding.AwayFromZero));
            int numberScout = Convert.ToInt32(Math.Round(totalNumberBees * 0.26, 0, MidpointRounding.ToEven));
            int numberActive = totalNumberBees - numberScout - numberInactive;
            int maxNumberVisits = 5; 
            int maxNumberCycles = 1000;
            CallAll(totalNumberBees, numberInactive, numberActive, numberScout, maxNumberVisits, maxNumberCycles, citiesData);

        }
        static void CallAll(int totalNumberBees, int numberInactive, int numberActive, int numberScout, int maxNumberVisits, int maxNumberCycles, CitiesData citiesData)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            SBC sbc = new SBC(totalNumberBees, numberInactive, numberActive, numberScout, maxNumberVisits, maxNumberCycles, citiesData);
            Console.WriteLine($"Random path {sbc}");
            sbc.Solve(true);
            Console.WriteLine($"Final path {sbc}");
            sw.Stop();
            Console.WriteLine((sw.ElapsedMilliseconds / 100.0).ToString());
        }
    }
    class SBC {
        private class Bee {
            public int status;
            public int[] memoryMatrix;
            public double randomPathSum; 
            public int numberOfVisits;

            public Bee(int status, int[] memoryMatrix, double measureOfQuality, int numberOfVisits) {
                this.status = status;
                this.memoryMatrix = new int[memoryMatrix.Length];
                Array.Copy(memoryMatrix, this.memoryMatrix, memoryMatrix.Length);
                this.randomPathSum = measureOfQuality;
                this.numberOfVisits = numberOfVisits;
            }
        }

        static Random random = null;

        private CitiesData citiesData;

        private int totalNumberBees; 
        private int numberInactive; 
        private int numberActive;
        private int numberScout;

        private int maxNumberCycles;
        private int maxNumberVisits; 

        private double probPersuasion = 0.90;
        private double probMistake = 0.01;
        
        private Bee[] bees;
        private int[] bestMemoryMatrix;
        public double bestSumPath;
        private int[] indexesOfInactiveBees;
        public SBC(int totalNumberBees, int numberInactive, int numberActive, int numberScout, int maxNumberVisits, int maxNumberCycles, CitiesData citiesData) {

            random = new Random(0);
      
            this.totalNumberBees = totalNumberBees;
            this.numberInactive = numberInactive;
            this.numberActive = numberActive;
            this.numberScout = numberScout;
            this.maxNumberVisits = maxNumberVisits;
            this.maxNumberCycles = maxNumberCycles;

            this.citiesData = citiesData;

            this.bees = new Bee[totalNumberBees];
            this.bestMemoryMatrix = GenerateRandomMatrix();
            this.bestSumPath = SumPath(this.bestMemoryMatrix);
            this.indexesOfInactiveBees = new int[numberInactive]; 

            for (int i = 0; i < totalNumberBees; ++i) {
                int currStatus; 
                if (i < numberInactive) {
                    currStatus = 0; 
                    indexesOfInactiveBees[i] = i; 
                }
                else if (i < numberInactive + numberScout)
                    currStatus = 2;
                else
                    currStatus = 1; 
    
                int[] randomMemoryMatrix = GenerateRandomMatrix();
                double mq = SumPath(randomMemoryMatrix);
                int numberOfVisits = 0;

                bees[i] = new Bee(currStatus, randomMemoryMatrix, mq, numberOfVisits); 
        
                if (bees[i].randomPathSum < bestSumPath) {
                    Array.Copy(bees[i].memoryMatrix, this.bestMemoryMatrix, 
                        bees[i].memoryMatrix.Length);
                    this.bestSumPath = bees[i].randomPathSum;
                }
            } 
        }
        public override string ToString()
        {
            string s = "";
            for (int i = 0; i < this.bestMemoryMatrix.Length; ++i)
                s += this.bestMemoryMatrix[i] + " ";
            return $"{bestSumPath}";
            //return $"bees <{totalNumberBees}> = {bestSumPath}";
        }
        private int[] GenerateRandomMatrix() {
            int[] result = new int[this.citiesData.cities.Length];
            Array.Copy(this.citiesData.cities, result, this.citiesData.cities.Length);      
            for (int i = 0; i < result.Length; i++) {
                int r = random.Next(i, result.Length);
                int temp = result[r];
                result[r] = result[i];
                result[i] = temp;
            }
            return result;
        }
        private int[] SearchAdjResult(int[] memoryMatrix) {
            int[] result = new int[memoryMatrix.Length];
            Array.Copy(memoryMatrix, result, memoryMatrix.Length);
            int ranIndex = random.Next(0, result.Length);
            int adjIndex;
            if (ranIndex == result.Length - 1)
                adjIndex = 0;
            else
                adjIndex = ranIndex + 1;
            int tmp = result[ranIndex];
            result[ranIndex] = result[adjIndex];
            result[adjIndex] = tmp;  
            return result;
        }
        private double SumPath(int[] memoryMatrix) {
            double answer = 0.0;
            for (int i = 0; i < memoryMatrix.Length - 1; ++i) {
                int c1 = memoryMatrix[i];
                int c2 = memoryMatrix[i + 1];
                double d = this.citiesData.Distance(c1, c2);
                answer += d;
            }
            return answer;
        }
        public void Solve(bool doProgressBar) {
            bool pb = doProgressBar;
            //int numberOfSymbolsToPrint = 29; 
            //int increment = this.maxNumberCycles / numberOfSymbolsToPrint;
            //if (pb) Console.Write("<");
            int cycle = 0;
      
            while (cycle < this.maxNumberCycles) {
                for (int i = 0; i < totalNumberBees; ++i) {
                    if (this.bees[i].status == 1)
                        ProcessActiveBee(i);
                    else if (this.bees[i].status == 2)
                        ProcessScoutBee(i);
                } 
                ++cycle;

                //if (pb && cycle % increment == 0)
                //    Console.Write("*");
            } 

            //if (pb) Console.Write(">\n");
        }
        private void ProcessActiveBee(int i) {
            int[] resultAdjPath = SearchAdjResult(bees[i].memoryMatrix);
            double resultAdjSum = SumPath(resultAdjPath); 
            double prob = random.NextDouble();
            bool memoryWasUpdated = false;
            bool numberOfVisitsOverLimit = false; 

            if (resultAdjSum < bees[i].randomPathSum) {
                if (prob < probMistake) {
                    ++bees[i].numberOfVisits;
                    if (bees[i].numberOfVisits > maxNumberVisits)
                        numberOfVisitsOverLimit = true;
                }
                else {
                    Array.Copy(resultAdjPath, bees[i].memoryMatrix, resultAdjPath.Length);
                    bees[i].randomPathSum = resultAdjSum;
                    bees[i].numberOfVisits = 0; 
                    memoryWasUpdated = true; 
                }
            }
            else {
                if (prob < probMistake) {
                    Array.Copy(resultAdjPath, bees[i].memoryMatrix, resultAdjPath.Length);
                    bees[i].randomPathSum = resultAdjSum;
                    bees[i].numberOfVisits = 0;
                    memoryWasUpdated = true; 
                }
                else {
                    ++bees[i].numberOfVisits;
                    if (bees[i].numberOfVisits > maxNumberVisits)
                        numberOfVisitsOverLimit = true;
                }
            }
            if (numberOfVisitsOverLimit == true) {
                bees[i].status = 0; 
                bees[i].numberOfVisits = 0;
                int x = random.Next(numberInactive); 
                bees[indexesOfInactiveBees[x]].status = 1; 
                indexesOfInactiveBees[x] = i; 
            }
            else if (memoryWasUpdated == true) {
                if (bees[i].randomPathSum < this.bestSumPath) {
                    Array.Copy(bees[i].memoryMatrix, this.bestMemoryMatrix,
                        bees[i].memoryMatrix.Length);
                    this.bestSumPath = bees[i].randomPathSum;
                }
                DoWaggleDance(i);
            }
            else 
            {
                return;
            }
        }
        private void ProcessScoutBee(int i) {
            int[] randomPath = GenerateRandomMatrix();
            double randomPathSum = SumPath(randomPath);
            if (randomPathSum < bees[i].randomPathSum) {
                Array.Copy(randomPath, bees[i].memoryMatrix, randomPath.Length); bees[i].randomPathSum = randomPathSum;
                if (bees[i].randomPathSum < bestSumPath) {
                    Array.Copy(bees[i].memoryMatrix, this.bestMemoryMatrix, bees[i].memoryMatrix.Length);
                    this.bestSumPath = bees[i].randomPathSum;
                } 
                DoWaggleDance(i);
            }
        }
        private void DoWaggleDance(int i) {
            for (int ii = 0; ii < numberInactive; ++ii) {
                int b = indexesOfInactiveBees[ii]; 
                if (bees[i].randomPathSum < bees[b].randomPathSum) {
                    double p = random.NextDouble(); 
                    if (this.probPersuasion > p) {
                        Array.Copy(bees[i].memoryMatrix, bees[b].memoryMatrix, bees[i].memoryMatrix.Length);
                        bees[b].randomPathSum = bees[i].randomPathSum;
                    } 
                } 
            } 
        }
    }
    class CitiesData {
        public int[] cities;
        private int[,] matrixA;
        public CitiesData(int numberCities) {
            //this.Write(numberCities);
            this.cities = new int[numberCities];
            for (int i = 0; i < this.cities.Length; ++i)
                this.cities[i] = i+1;
            this.matrixA = AdjMatrix();
        }
        public double Distance(int firstCity, int secondCity)
        {
            return matrixA[firstCity - 1, secondCity - 1];
        }
        public double FirstPathSum()
        {
            int path = 0;
            for (int i = 0; i < cities.Length; i++)
                if (i < cities.Length - 1)
                    path += matrixA[cities[i] - 1, cities[i + 1] - 1];
                else
                    path += matrixA[cities[i] - 1, 0];
            return path;
        }
        private int[,] AdjMatrix()
        {
            List<((int, int), int)> list = Read();
            int[,] matrixA = new int[this.cities.Length, this.cities.Length];
            for (int i = 0; i < list.Count; i++)
                matrixA[list[i].Item1.Item1 - 1, list[i].Item1.Item2 - 1] = list[i].Item2;
            return matrixA;
        }
        private List<((int, int), int)> Read()
        {
            List<((int, int), int)> result = new List<((int, int), int)>();
            using (StreamReader sr = new StreamReader("index.txt", System.Text.Encoding.Default))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                    if (line != Convert.ToString(this.cities.Length))
                    {
                        ((int, int), int) temp = ConvertForRead(line);
                        result.Add(temp);
                    }
            }

            return result;
        }
        private ((int, int), int) ConvertForRead(string line)
        {
            string[] array = line.Split(' ');
            ((int, int), int) result = ((Convert.ToInt32(array[0]), Convert.ToInt32(array[1])), Convert.ToInt32(array[2]));
            return result;
        }
        public void Write(int size)
        {
            Random random = new Random();
            using (StreamWriter sw = new StreamWriter("index.txt", false, System.Text.Encoding.Default))
            {
                sw.WriteLine($"{size}");
                for (int i = 1; i <= size; i++)
                    for (int j = 1; j <= size; j++)
                        if (i == j)
                            sw.WriteLine($"{i} {j} {0}");
                        else
                            sw.WriteLine($"{i} {j} {random.Next(5, 150)}");
            }
        }
        public override string ToString() {
            string s = "";
            s += "Cities: ";
            for (int i = 0; i < this.cities.Length; ++i)
                s += this.cities[i] + " ";
            return s;
        }
    }
}