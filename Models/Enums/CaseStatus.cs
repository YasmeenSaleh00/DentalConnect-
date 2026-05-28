namespace DentBridge.Models.Enums;

public enum CaseStatus
{
    Open = 0,
    UnderReview = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum AccountStatus
{
    Pending = 0,
    Active = 1,
    Rejected = 2,
    Suspended = 3
}

public enum TreatmentType
{
    GeneralDentistry = 0,
    Orthodontics = 1,
    Prosthodontics = 2,
    Periodontics = 3,
    Endodontics = 4,
    OralSurgery = 5,
    Pedodontics = 6,
    Cosmetic = 7
}

public enum NotificationType
{
    CaseAccepted = 0,
    CaseCompleted = 1,
    AccountApproved = 2,
    AccountRejected = 3,
    NewCase = 4,
    ReviewReceived = 5,
    General = 6,
    TestimonialApproved = 7,
    TestimonialRejected = 8
}

public enum TestimonialStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
