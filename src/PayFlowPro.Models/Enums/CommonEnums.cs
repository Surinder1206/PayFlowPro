namespace PayFlowPro.Models.Enums;

public enum Gender
{
    Male = 0,
    Female = 1,
    Other = 2
}

public enum MaritalStatus
{
    Single = 0,
    Married = 1,
    Divorced = 2,
    Widowed = 3
}

public enum EmploymentStatus
{
    Active = 0,
    Inactive = 1,
    Terminated = 2,
    OnLeave = 3,
    Resigned = 4
}

public enum PayslipStatus
{
    Draft = 0,
    Generated = 1,
    Approved = 2,
    Sent = 3,
    Paid = 4
}

public enum AllowanceType
{
    Fixed = 0,
    Percentage = 1,
    Formula = 2
}

public enum DeductionType
{
    Fixed = 0,
    Percentage = 1,
    Tax = 2,
    Insurance = 3
}

public enum PayFrequency
{
    Monthly = 0,
    BiWeekly = 1,
    Weekly = 2,
    Quarterly = 3,
    Annual = 4
}