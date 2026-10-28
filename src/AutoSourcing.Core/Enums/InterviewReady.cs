namespace AutoSourcing.Core.Enums;

// The fixed interview-ready criteria (from the Interview-Ready Talent Strategy). The order and set
// are fixed; the job supplies the detail behind each one.
public enum InterviewReadyCriterion
{
    RoleRelevance = 0,
    RoleUnderstanding = 1,
    WorkHours = 2,
    Location = 3,
    Pay = 4,
    WorkAuthorisation = 5,
    BackgroundChecks = 6,
    Licences = 7,
    Interest = 8,
    HumanConfirmation = 9,
    InterviewArranged = 10
}

public enum InterviewReadyStatus
{
    Unknown = 0,
    Met = 1,
    NotMet = 2,
    NotApplicable = 3
}

public enum CheckSource
{
    Agent = 0,
    Human = 1
}