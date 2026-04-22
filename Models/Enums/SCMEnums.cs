namespace SCM_System.Models.Enums
{
    public enum StorageType
    {
        General,
        ColdStorage,
        DryStorage
    }

    public enum WarehouseStatus
    {
        Active,
        Inactive
    }

    public enum VehicleType
    {
        Truck,
        Van,
        Motorcycle,
        Bicycle,
        TukTuk,
        SmallVan,
        PickupTruck,
        BoxTruck,
        RefrigeratedTruck,
        FlatbedTruck,
        SmallCargoTruck,
        MediumCargoTruck,
        LargeCargoTruck,
        SemiTrailerTruck,
        ContainerTruck,
        FuelTanker,
        WaterTanker,
        LivestockTruck,
        HazardousMaterialsTruck,
        MobileCrane,
        Forklift
    }

    public enum VehicleStatus
    {
        Available,      // Ready for assignment
        InUse,          // Currently on delivery
        Maintenance,    // Being serviced
        OutOfService,   // Broken/retired
        Retired,        // Formally decommissioned
        Missing,        // Lost/stolen (rare)
        Inactive        // Soft-deleted
    }

    public enum HubType
    {
        Warehouse,
        DistributionCenter,
        FulfillmentCenter,
        ColdStorage,
        CrossDock
    }

    public enum StorageArchitecture
    {
        General,
        Bulk,
        Secure,
        ColdStorage,
        RackStorage,
        Hazardous,
        Perishable
    }

    public enum EmploymentType
    {
        FullTime,
        Contract
    }

    public enum ShiftType
    {
        Day,
        Night,
        Morning,
        Afternoon,
        Flexible,
        Rotating
    }

    public enum EmployeeStatus
    {
        Active,
        Inactive,
        OnLeave,
        Suspended
    }

    public enum LicenseType
    {
        Public,
        Commercial,
        HeavyVehicle
    }

    public enum IncidentType
    {
        Breakdown,
        Accident,
        Damage,
        Loss,
        Delay,
        TheftRisk,
        Other
    }

    public enum IncidentSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum TransferStatus
    {
        Requested,
        Approved,
        InTransit,
        Received,
        Cancelled
    }

    public enum PerformanceRating
    {
        Outstanding,
        Good,
        Average,
        BelowAverage,
        Poor
    }

    public enum OccupancyStatus
    {
        Normal,
        NearingCapacity,
        Full,
        Overflow
    }
}
