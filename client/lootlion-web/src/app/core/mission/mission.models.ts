export enum MissionAssignmentMode {
  DirectAssign = 'DirectAssign',
  BoardClaim = 'BoardClaim',
}

export enum MissionRecurrenceKind {
  None = 'None',
  Daily = 'Daily',
  Weekly = 'Weekly',
  Monthly = 'Monthly',
  IntervalDays = 'IntervalDays',
}

export enum MissionInstanceStatus {
  Available = 'Available',
  Active = 'Active',
  Submitted = 'Submitted',
  Approved = 'Approved',
  Rejected = 'Rejected',
  Cancelled = 'Cancelled',
}

export interface MissionTemplateDto {
  id?: string;
  householdId?: string;
  createdByUserId?: string;
  title?: string | null;
  description?: string | null;
  rewardExp?: number;
  rewardCoin?: number;
  requiresApproval?: boolean;
  assignmentMode?: MissionAssignmentMode;
  defaultAssigneeUserId?: string | null;
  recurrenceKind?: MissionRecurrenceKind;
  recurrenceIntervalDays?: number | null;
  recurrenceDayOfWeek?: string | null;
  recurrenceDayOfMonth?: number | null;
  isActive?: boolean;
  canSpawnNextRound?: boolean;
  createdUtc?: string;
  cancelledUtc?: string | null;
}

export interface MissionInstanceDto {
  id?: string;
  templateId?: string;
  householdId?: string;
  assignedToUserId?: string | null;
  periodKey?: string;
  status?: MissionInstanceStatus;
  availableFromUtc?: string;
  submittedUtc?: string | null;
  completedUtc?: string | null;
  title?: string | null;
  description?: string | null;
  rewardExp?: number;
  rewardCoin?: number;
  requiresApproval?: boolean;
  assignmentMode?: MissionAssignmentMode;
  recurrenceKind?: MissionRecurrenceKind;
}

export interface CreateMissionTemplateRequest {
  householdId?: string;
  title?: string | null;
  description?: string | null;
  rewardExp?: number;
  rewardCoin?: number;
  requiresApproval?: boolean;
  assignmentMode?: MissionAssignmentMode;
  defaultAssigneeUserId?: string | null;
  recurrenceKind?: MissionRecurrenceKind;
  recurrenceIntervalDays?: number | null;
  recurrenceDayOfWeek?: string | null;
  recurrenceDayOfMonth?: number | null;
}
