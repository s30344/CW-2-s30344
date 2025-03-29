

public class OverfillException : Exception
{
    public OverfillException(string message):base(message){ }    
}

public interface IHazardNotifier
{
    void Notify(string containerNr);
}

public enum ContainerType
{
    Liquid,
    Gas,
    Refrigerated
}

public enum ProductType
{
    Fruit,
    Meat,
    Dairy,
}

public static class ProductTemperature
{
    public static Dictionary<ProductType, double> StorageTemperatures = new Dictionary<ProductType, double>
    {
        {ProductType.Dairy, 7.2 },
        {ProductType.Fruit, 13.3 },
        {ProductType.Meat, -15 }
    };
}

public abstract class Container
{
 static int counter = 0;
 
 public string SerialNumber { get; }
 public double LoadMass { get; set; }
 public double Height { get; }
 public double EmptyWeight { get; }
 public double Depth { get; }
 public double MaxPayload { get; }
 public ContainerType CT { get; set; }

 protected Container(double height, double depth, double maxPayload, double emptyWeight, string TypeCode)
 {
     Height = height;
     EmptyWeight = emptyWeight;
     Depth = depth;
     MaxPayload = maxPayload;
     SerialNumber = $"KON-{TypeCode}-{counter++}";
 }

 public virtual void EmptyContainer()
 {
     LoadMass = 0;
 }

 public virtual void LoadContainer(double mass)
 {
     if (mass > MaxPayload)
     {
         throw new OverfillException("Overfill");
     }
     LoadMass = mass;
 }
 public double TotalWeight(){ return EmptyWeight + LoadMass;}

 public virtual string Info()
 {
     return $"Serial number: {SerialNumber} Type: {CT} Load: {LoadMass}kg Total Weight: {TotalWeight()}kg Max payload: {MaxPayload}kg";
 }
}
public class LiquidContainer : Container, IHazardNotifier
{
    public bool Hazardous { get; }

    public LiquidContainer(double height, double depth, double emptyWeight, bool hazardous, double maxPayload)
    : base(height, depth, maxPayload, emptyWeight,  "L")
    {
        CT = ContainerType.Liquid;
        Hazardous = hazardous;
    }

    public void Notify(string containerNr)
    {
        Console.WriteLine($"Hazard warning in liquid container {containerNr}");
    }

    public override void LoadContainer(double mass)
    {
        double max = Hazardous ? MaxPayload * 0.5 : EmptyWeight * 0.9;

        if (mass > max)
        {
            Notify(SerialNumber);
            throw new OverfillException("Overfill");
        }
    }

    public override string Info()
    {
        return base.Info() + $", Hazardous: {Hazardous}";
    }
}

public class GasContainer : Container, IHazardNotifier
{
    public double Pressure { get; }

    public GasContainer(double height, double depth, double maxPayload, double emptyWeight, double pressure) : base(
        height, depth, maxPayload, emptyWeight, "G")
    {
        CT = ContainerType.Gas;
        Pressure = pressure;
    }
    public void Notify(string containerNr)
    {
        Console.WriteLine($"Hazard warning in gas container {containerNr}");
    }
    public override void EmptyContainer()
    {
        LoadMass = MaxPayload*0.05;
    }

    public override void LoadContainer(double mass)
    {
        if (mass > MaxPayload)
        {
            Notify(SerialNumber);
            throw new OverfillException("Overfill");
        }
        LoadMass = mass;
    }

    public override string Info()
    {
        return base.Info() + $", Pressure: {Pressure}";
    }
}

public class RefrigeratedContainer : Container
{
    public ProductType Product { get; }
    public double Temperature { get; }

    public RefrigeratedContainer(double height, double depth, double maxPayload, double emptyWeight,
        ProductType product, double temperature) : base(height, depth, maxPayload, emptyWeight, "R")
    {
        if (temperature < ProductTemperature.StorageTemperatures[product])
        {
            throw new ArgumentException("Temperature in refrigerated container is too low");
        }
        CT = ContainerType.Refrigerated;
        Product = product;
        Temperature = temperature;
    }
    public override string Info()
    {
        return base.Info() + $", Product: {Product}, Temperature: {Temperature}";
    }
}

public class ContainerShip
{
    public List<Container> Containers { get; } = new List<Container>();
    public double MaxSpeed { get; }
    public int MaxContainerCount { get; }
    public double MaxTotalWeight { get; }

    public ContainerShip(double maxSpeed, int maxContainerCount, double maxTotalWeight)
    {
        MaxSpeed = maxSpeed;
        MaxContainerCount = maxContainerCount;
        MaxTotalWeight = maxTotalWeight;
    }

public void LoadContainer(Container container)
    {
        if (Containers.Count >= MaxContainerCount)
        {
            throw new InvalidOperationException($"Cannot load container {container.SerialNumber}, ship has reached max container count");
        }

        double totalWeightAfterLoading = Containers.Sum(c => c.TotalWeight()) + container.TotalWeight();
        if (totalWeightAfterLoading > MaxTotalWeight * 1000)
        {
            throw new InvalidOperationException($"Cannot load container {container.SerialNumber}, ship would exceed max weight");
        }

        Containers.Add(container);
    }

    public void LoadContainers(List<Container> containers)
    {
        foreach (var container in containers)
        {
            LoadContainer(container);
        }
    }

    public void UnloadContainer(string serialNumber)
    {
        var container = Containers.FirstOrDefault(c => c.SerialNumber == serialNumber);
        if (container != null)
        {
            Containers.Remove(container);
        }
        else
        {
            throw new KeyNotFoundException($"Container with serial number {serialNumber} not found on the ship");
        }
    }

    public void ReplaceContainer(string serialNumber, Container newContainer)
    {
        var index = Containers.FindIndex(c => c.SerialNumber == serialNumber);
        if (index == -1)
        {
            throw new KeyNotFoundException($"Container with serial number {serialNumber} not found on the ship");
        }

        Containers[index] = newContainer;
    }

    public void TransferContainer(string serialNumber, ContainerShip targetShip)
    {
        var container = Containers.FirstOrDefault(c => c.SerialNumber == serialNumber);
        if (container == null)
        {
            throw new KeyNotFoundException($"Container with serial number {serialNumber} not found on the ship");
        }

        UnloadContainer(serialNumber);
        targetShip.LoadContainer(container);
    }

    public string GetShipInfo()
    {
        double totalWeight = Containers.Sum(c => c.TotalWeight()) / 1000; 
        return $"Ship info: Speed {MaxSpeed} knots, Containers: {Containers.Count}/{MaxContainerCount}, " +
               $"Weight: {totalWeight:F2}/{MaxTotalWeight}t";
    }

    public void PrintContainersInfo()
    {
        Console.WriteLine($"Containers on ship ({Containers.Count}):");
        foreach (var container in Containers)
        {
            Console.WriteLine(container.Info());
        }
    }
}