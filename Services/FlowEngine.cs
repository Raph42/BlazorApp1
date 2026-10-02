// Parcourt les liaisons du diagramme et propage les flux :

using Blazor.Diagrams;
using Blazor.Diagrams.Core;
using Blazor.Diagrams.Core.Anchors;
using CaptainArchitect.Models;

namespace CaptainArchitect.Services;

public static class FlowEngine
{
    public static void RecalculateFlows(BlazorDiagram diagram)
    {
        var machineNodes = diagram.Nodes.OfType<MachineNodeModel>().ToList();

        // 1. Réinitialiser les débits entrants et sortants distribués
        foreach (var node in machineNodes)
        {
            node.ActualInputRates.Clear();
            node.DistributedOutputRates.Clear();
            node.DownstreamDemandRates.Clear();
            node.OperationalEfficiency = 1.0m;

            foreach (var port in node.Ports.OfType<ResourcePortModel>())
            {
                if (port.Direction == PortDirection.Input)
                    node.ActualInputRates[port.Id] = 0m;
                else
                {
                    node.DistributedOutputRates[port.Id] = 0m;
                    node.DownstreamDemandRates[port.Id] = 0m;
                }
            }
        }


        // 2. Boucle de propagation (2 à 3 passes pour propager d'amont en aval)
        int iterations = Math.Max(3, machineNodes.Count);

        for (int iter = 0; iter < iterations; iter++)
        {
            // Réinitialiser les entrées avant chaque passe
            foreach (var node in machineNodes)
            {
                foreach (var port in node.Ports.OfType<ResourcePortModel>().Where(p => p.Direction == PortDirection.Input))
                {
                    node.ActualInputRates[port.Id] = 0m;
                }
            }

            // Distribution de la production réelle vers les entrées
            foreach (var sourceNode in machineNodes)
            {
                for (int i = 0; i < sourceNode.Recipe.Outputs.Count; i++)
                {
                    var outputItem = sourceNode.Recipe.Outputs[i];
                    var outputPort = sourceNode.Ports.OfType<ResourcePortModel>()
                        .FirstOrDefault(p => p.Direction == PortDirection.Output && p.ResourceId == outputItem.ResourceId);

                    if (outputPort == null) continue;

                    // PRODUCTION RÉELLE (bridée par l'alimentation de la machine)
                    decimal actualProduction = sourceNode.GetActualProducedRatePerMin(outputItem);

                    // Trouver les ports cibles
                    var connectedTargetPorts = new List<ResourcePortModel>();
                    foreach (var link in diagram.Links)
                    {
                        ResourcePortModel? srcPort = (link.Source as SinglePortAnchor)?.Port as ResourcePortModel;
                        ResourcePortModel? tgtPort = (link.Target as SinglePortAnchor)?.Port as ResourcePortModel;

                        if (srcPort == outputPort && tgtPort is { Direction: PortDirection.Input })
                            connectedTargetPorts.Add(tgtPort);
                        else if (tgtPort == outputPort && srcPort is { Direction: PortDirection.Input })
                            connectedTargetPorts.Add(srcPort);
                    }

                    if (connectedTargetPorts.Count == 0) continue;

                    // Calcul de la demande totale
                    decimal totalDemand = 0m;
                    var targetDemands = new Dictionary<ResourcePortModel, decimal>();

                    foreach (var targetPort in connectedTargetPorts)
                    {
                        if (targetPort.Parent is MachineNodeModel targetNode)
                        {
                            var reqItem = targetNode.Recipe.Inputs.FirstOrDefault(inp => inp.ResourceId == targetPort.ResourceId);
                            decimal demand = reqItem != null ? targetNode.GetInputRatePerMin(reqItem) : 0m;
                            targetDemands[targetPort] = demand;
                            totalDemand += demand;
                        }
                    }

                    sourceNode.DownstreamDemandRates[outputPort.Id] = totalDemand;

                    // Répartition au prorata de la production réelle disponible
                    decimal distributedTotal = 0m;
                    foreach (var targetPort in connectedTargetPorts)
                    {
                        decimal demand = targetDemands[targetPort];
                        decimal flow = 0m;

                        if (totalDemand > 0)
                        {
                            if (actualProduction >= totalDemand)
                                flow = demand;
                            else
                                flow = actualProduction * (demand / totalDemand);
                        }

                        if (targetPort.Parent is MachineNodeModel targetNode)
                        {
                            targetNode.ActualInputRates[targetPort.Id] += flow;
                        }
                        distributedTotal += flow;
                    }

                    sourceNode.DistributedOutputRates[outputPort.Id] = distributedTotal;
                }
            }

            // Calcul du taux d'activité (Efficiency) de chaque machine
            foreach (var node in machineNodes)
            {
                // On ne prend en compte que les ports d'entrée EFFECTIVEMENT reliés à un tuyau
                var connectedInputPorts = node.Ports.OfType<ResourcePortModel>()
                    .Where(p => p.Direction == PortDirection.Input && p.Links.Count > 0)
                    .ToList();

                // Si aucune entrée n'est reliée, la machine est considérée disponible à 100% de sa capacité théorique
                if (connectedInputPorts.Count == 0)
                {
                    node.OperationalEfficiency = 1.0m;
                    continue;
                }

                decimal minRatio = 1.0m;
                foreach (var port in connectedInputPorts)
                {
                    var inputItem = node.Recipe.Inputs.FirstOrDefault(i => i.ResourceId == port.ResourceId);
                    if (inputItem != null)
                    {
                        decimal required = node.GetInputRatePerMin(inputItem);
                        decimal actual = node.GetActualInputRate(port.Id);

                        decimal ratio = required > 0 ? (actual / required) : 1.0m;
                        if (ratio < minRatio) minRatio = ratio;
                    }
                }

                node.OperationalEfficiency = Math.Clamp(minRatio, 0m, 1m);
            }

        }


        // Notifier les nœuds pour le rafraîchissement graphique
        foreach (var node in machineNodes)
        {
            node.Refresh();
        }

    }
}