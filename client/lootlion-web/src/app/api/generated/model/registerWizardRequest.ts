/** สอดคล้องกับ RegisterWizardRequest ฝั่ง API */
export interface RegisterWizardRequest {
  nickname?: string | null;
  /** 0 = Parent, 1 = Child */
  role?: number;
  createNewHousehold?: boolean;
  newHouseholdName?: string | null;
  joinHouseholdIdAsParent?: string | null;
  userName?: string | null;
  email?: string | null;
  password?: string | null;
  joinHouseholdIdAsChild?: string | null;
}
