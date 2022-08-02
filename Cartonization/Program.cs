// Solve a multiple knapsack problem using a MIP solver.
using System;
using System.Collections.Generic;
using System.Linq;
using Google.OrTools.LinearSolver;
using Google.OrTools.Sat;
using Constraint = Google.OrTools.LinearSolver.Constraint;
using LinearExpr = Google.OrTools.Sat.LinearExpr;

public class Program
{
    public static void Main()
    {
        // Cartonize();
        Cartonize2();
    }

    private static void Cartonize()
    {
        // Instantiate the data problem.
        double[] Weights = { 48, 30, 42, 36, 36, 48, 42, 42, 36, 24, 30, 30, 42, 36, 36 };
        double[] Values = { 10, 30, 25, 50, 35, 30, 15, 40, 30, 35, 45, 10, 20, 30, 25 };
        int NumItems = Weights.Length;
        int[] allItems = Enumerable.Range(0, NumItems).ToArray();

        double[] BinCapacities = { 100, 100, 100, 100, 100 };
        int NumBins = BinCapacities.Length;
        int[] allBins = Enumerable.Range(0, NumBins).ToArray();

        // Create the linear solver with the SCIP backend.
        Solver solver = Solver.CreateSolver("SCIP");

        // Variables.
        Variable[,] x = new Variable[NumItems, NumBins];
        foreach (int i in allItems)
        {
            foreach (int b in allBins)
            {
                x[i, b] = solver.MakeBoolVar($"x_{i}_{b}");
            }
        }

        // Constraints.
        // Each item is assigned to at most one bin.
        foreach (int i in allItems)
        {
            Constraint constraint = solver.MakeConstraint(0, 1, "");
            foreach (int b in allBins)
            {
                constraint.SetCoefficient(x[i, b], 1);
            }
        }

        // The amount packed in each bin cannot exceed its capacity.
        foreach (int b in allBins)
        {
            Constraint constraint = solver.MakeConstraint(0, BinCapacities[b], "");
            foreach (int i in allItems)
            {
                constraint.SetCoefficient(x[i, b], Weights[i]);
            }
        }

        // Objective.
        Objective objective = solver.Objective();
        foreach (int i in allItems)
        {
            foreach (int b in allBins)
            {
                objective.SetCoefficient(x[i, b], Values[i]);
            }
        }
        objective.SetMaximization();

        Solver.ResultStatus resultStatus = solver.Solve();

        // Check that the problem has an optimal solution.
        if (resultStatus == Solver.ResultStatus.OPTIMAL)
        {
            Console.WriteLine($"Total packed value: {solver.Objective().Value()}");
            double TotalWeight = 0.0;
            foreach (int b in allBins)
            {
                double BinWeight = 0.0;
                double BinValue = 0.0;
                Console.WriteLine("Bin " + b);
                foreach (int i in allItems)
                {
                    if (x[i, b].SolutionValue() == 1)
                    {
                        Console.WriteLine($"Item {i} weight: {Weights[i]} values: {Values[i]}");
                        BinWeight += Weights[i];
                        BinValue += Values[i];
                    }
                }
                Console.WriteLine("Packed bin weight: " + BinWeight);
                Console.WriteLine("Packed bin value: " + BinValue);
                TotalWeight += BinWeight;
            }
            Console.WriteLine("Total packed weight: " + TotalWeight);
        }
        else
        {
            Console.WriteLine("The problem does not have an optimal solution!");
        }
    }
    
    private static void Cartonize2()
    {
        // Instantiate the data problem.
        int[] Weights = { 48, 30, 42, 36, 36, 48, 42, 42, 36, 24, 30, 30, 42, 36, 36 };
        int[] Values = { 10, 30, 25, 50, 35, 30, 15, 40, 30, 35, 45, 10, 20, 30, 25 };
        int NumItems = Weights.Length;
        int[] allItems = Enumerable.Range(0, NumItems).ToArray();

        int[] BinCapacities = { 100, 100, 100, 100, 100 };
        int NumBins = BinCapacities.Length;
        int[] allBins = Enumerable.Range(0, NumBins).ToArray();

        // Model.
        CpModel model = new CpModel();

        // Variables.
        ILiteral[,] x = new ILiteral[NumItems, NumBins];
        foreach (int i in allItems)
        {
            foreach (int b in allBins)
            {
                x[i, b] = model.NewBoolVar($"x_{i}_{b}");
            }
        }

        // Constraints.
        // Each item is assigned to at most one bin.
        foreach (int i in allItems)
        {
            List<ILiteral> literals = new List<ILiteral>();
            foreach (int b in allBins)
            {
                literals.Add(x[i, b]);
            }
            model.AddAtMostOne(literals);
        }

        // The amount packed in each bin cannot exceed its capacity.
        foreach (int b in allBins)
        {
            List<ILiteral> items = new List<ILiteral>();
            foreach (int i in allItems)
            {
                items.Add(x[i, b]);
            }
            model.Add(LinearExpr.WeightedSum(items, Weights) <= BinCapacities[b]);
        }

        // Objective.
        LinearExprBuilder obj = LinearExpr.NewBuilder();
        foreach (int i in allItems)
        {
            foreach (int b in allBins)
            {
                obj.AddTerm(x[i, b], Values[i]);
            }
        }
        model.Maximize(obj);

        // Solve
        CpSolver solver = new CpSolver();
        CpSolverStatus status = solver.Solve(model);

        // Print solution.
        // Check that the problem has a feasible solution.
        if (status == CpSolverStatus.Optimal || status == CpSolverStatus.Feasible)
        {
            Console.WriteLine($"Total packed value: {solver.ObjectiveValue}");
            double TotalWeight = 0.0;
            foreach (int b in allBins)
            {
                double BinWeight = 0.0;
                double BinValue = 0.0;
                Console.WriteLine($"Bin {b}");
                foreach (int i in allItems)
                {
                    if (solver.BooleanValue(x[i, b]))
                    {
                        Console.WriteLine($"Item {i} weight: {Weights[i]} values: {Values[i]}");
                        BinWeight += Weights[i];
                        BinValue += Values[i];
                    }
                }
                Console.WriteLine("Packed bin weight: " + BinWeight);
                Console.WriteLine("Packed bin value: " + BinValue);
                TotalWeight += BinWeight;
            }
            Console.WriteLine("Total packed weight: " + TotalWeight);
        }
        else
        {
            Console.WriteLine("No solution found.");
        }

        Console.WriteLine("Statistics");
        Console.WriteLine($"  conflicts: {solver.NumConflicts()}");
        Console.WriteLine($"  branches : {solver.NumBranches()}");
        Console.WriteLine($"  wall time: {solver.WallTime()}s");
    }
}
