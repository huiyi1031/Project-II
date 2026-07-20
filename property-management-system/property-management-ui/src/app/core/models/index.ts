// ─── Core interfaces matching backend C# entities ───

export interface UserAccount {
  accountID: number;
  email: string;
  roleType: string;         // 'Occupant' | 'Technician' | 'PropertyManager'
  accountStatus: string;
  lastLogin?: string;
  createdAt: string;
}

export interface Occupant {
  occupantID: number;
  accountID: number;
  fullName: string;
  identificationNo: string;
  contactNumber: string;
  gender: string;
  age: number;
  occupantType: string;     // 'Owner' | 'Tenant' | 'Resident'
  occupantStatus: string;
  email?: string;
  unitID?: number;
  unitNumber?: string;
}

export interface PropertyManager {
  managerID: number;
  accountID: number;
  fullName: string;
  contactNumber: string;
  gender: string;
  age: number;
  position: string;
}

export interface Technician {
  technicianID: number;
  accountID: number;
  serviceTypeID: number;
  fullName: string;
  contactNumber: string;
  gender: string;
  age: number;
  experienceLevel: string;    // 'Junior' | 'Intermediate' | 'Senior'
  availabilityStatus: string; // 'Available' | 'Busy' | 'OffDuty'
  ranking: number;
  serviceTypeName?: string;
  email?: string;
}

export interface Property {
  propertyID: number;
  propertyName: string;
  propertyType: string;
  address: string;
  city: string;
  state: string;
  postcode: string;
}

export interface PropertyUnit {
  unitID: number;
  propertyID: number;
  unitNumber: string;
  floor: number;
  block: string;
  unitType: string;
  size: number;
  status: string;           // 'Occupied' | 'Vacant'
  propertyName?: string;
}

export interface Contract {
  contractID: number;
  occupantID: number;
  unitID: number;
  contractType: string;
  startDate: string;
  endDate?: string;
  isPrimaryOccupant: boolean;
  documentPath?: string;
  status: string;
  occupantName?: string;
  unitNumber?: string;
}

export interface ServiceType {
  serviceTypeID: number;
  name: string;
  description: string;
  basePrice: number;
}

export interface MaintenanceRequest {
  requestID: number;
  requestTitle: string;
  issueCategory: string;
  description: string;
  priorityLevel: string;    // 'Low' | 'Medium' | 'High'
  status: string;           // 'Pending' | 'Assigned' | 'InProgress' | 'Completed' | 'Cancelled'
  submissionDate: string;
  preferredScheduleDate?: string;
  attachmentPath?: string;
  unitID: number;
  occupantID: number;
  unitNumber?: string;
  occupantName?: string;
  assignedTechnicianName?: string;
  scheduledDate?: string;
  workOrderID?: number;
}

export interface WorkOrder {
  workOrderID: number;
  requestID?: number;
  planID?: number;
  maintenanceType: string;  // 'Corrective' | 'Preventive'
  status: string;           // 'Assigned' | 'InProgress' | 'Completed' | 'Closed' | 'Cancelled'
  scheduledDate: string;
  completionDate?: string;
  priorityLevel: string;
  workReport?: string;
  issueTitle?: string;
  unitNumber?: string;
  technicianName?: string;
}

export interface WorkAssignment {
  assignmentID: number;
  workOrderID: number;
  technicianID: number;
  assignedDate: string;
  status: string;
  declineReason?: string;
  technicianName?: string;
}

export interface Asset {
  assetID: number;
  propertyID: number;
  assetName: string;
  assetType: string;
  location: string;
  installationDate: string;
  manufacturer: string;
  modelNumber: string;
  expLifespanYears: number;
  criticalityLevel: number;
  maintenanceIntervalDays: number;
  status: string;
  isHighRisk: boolean;
  riskLevel: string;        // 'High' | 'Medium' | 'Low'
  riskScore: number;
  qrCode?: string;
  propertyName?: string;
}

export interface MaintenancePlan {
  planID: number;
  assetID: number;
  planName: string;
  frequency: string;
  nextScheduledDate: string;
  lastExecutedDate?: string;
  estimatedCost: number;
  status: string;
  assetName?: string;
}

export interface AssetMaintenanceHistory {
  historyID: number;
  assetID: number;
  workOrderID: number;
  maintenanceType: string;
  failureType?: string;
  description: string;
  downtimeDuration?: number;
  cost: number;
  maintenanceDate: string;
  resultStatus: string;
  assetName?: string;
}

export interface Chat {
  chatID: number;
  requestID: number;
  createdAt: string;
  requestTitle?: string;
  requestStatus?: string;
  lastMessage?: string;
  participantCount?: number;
}

export interface ChatParticipant {
  participantID: number;
  chatID: number;
  accountID: number;
  fullName?: string;
  role?: string;
}

export interface Message {
  messageID: number;
  chatID: number;
  senderAccountID: number;
  content: string;
  sentAt: string;
  attachmentPath?: string;
  senderName?: string;
  isOwn?: boolean;
}

export interface Payment {
  paymentID: number;
  requestID: number;
  amount: number;
  paymentDate: string;
  paymentMethod: string;
  status: string;
  referenceNumber?: string;
}

// ─── Dashboard DTOs ───

export interface DashboardStats {
  openRequests:    number;
  inProgress:      number;
  completed:       number;
  fourthKpiLabel:  string;
  fourthKpiValue:  string | number;
  recentCases:     RecentCase[];
}

export interface RecentCase {
  caseID:    string;
  title:     string;
  status:    string;
  scheduled: string;
}

// ─── Create/Update DTOs ───

export interface CreateMaintenanceRequestDto {
  requestTitle:          string;
  issueCategory:         string;
  description:           string;
  priorityLevel:         string;
  unitID:                number;
  preferredScheduleDate?: string;
}

export interface CreateWorkOrderDto {
  requestID?:    number;
  planID?:       number;
  technicianID:  number;
  scheduledDate: string;
  priorityLevel: string;
}

export interface CreateStaffDto {
  fullName:           string;
  email:              string;
  contactNumber:      string;
  roleType:           string;
  serviceTypeID?:     number;
  priorityRank?:      number;
  experienceLevel?:   string;
  availabilityStatus?: string;
}

export interface UpdateStaffDto {
  serviceTypeID?:     number;
  priorityRank?:      number;
  availabilityStatus?: string;
  experienceLevel?:   string;
}

// ─── Auth / Login DTOs ────────────────────────────────────────────────────────

/** Returned by POST /api/Auth/check-email */
export interface LoginCheckResult {
  found:               boolean;
  roleType?:           string;   // 'Occupant' | 'Technician' | 'PropertyManager'
  occupantType?:       string;   // 'Owner' | 'Tenant' | 'Resident'
  accountStatus?:      string;   // 'Active' | 'Pending' | 'Deactivated'
  /** 'None' = no password yet (first login), 'Temporary' = staff temp pw, 'Active' = normal */
  passwordStatus?:     'None' | 'Temporary' | 'Active';
}

/** Returned by POST /api/Auth/verify-ic */
export interface VerifyIcResult {
  found:         boolean;
  maskedEmail?:  string;      // e.g. "r****@gmail.com" shown to user for confirmation
  updateToken?:  string;      // Short-lived token (15min) that allows email update only
}

export interface SetPasswordDto {
  email:           string;
  newPassword:     string;
  confirmPassword: string;
  /** Included only when setting password after IC bypass (owner flow) */
  updateToken?:    string;
}

export interface VerifyTempPasswordDto {
  email:            string;
  temporaryPassword: string;
}

// ─── Family Member DTOs ───────────────────────────────────────────────────────

export interface FamilyMember {
  occupantID:       number;
  fullName:         string;
  identificationNo: string;
  relationship:     string;    // 'Spouse' | 'Child' | 'Parent' | 'Sibling' | 'Other'
  email:            string;
  contactNumber:    string;
  gender:           string;
  dateOfBirth:      string;
  occupantStatus:   string;    // 'Active' | 'Pending'
}

export interface AddFamilyMemberDto {
  fullName:         string;
  identificationNo: string;
  relationship:     string;
  email:            string;
  contactNumber:    string;
  gender:           string;
  dateOfBirth:      string;
}

/** Returned by GET /api/PropertyUnits/my/headcount */
export interface UnitHeadcount {
  currentCount:  number;
  maxOccupants:  number;
  unitNumber:    string;
  canAddDirect:  boolean;   // true if currentCount < maxOccupants
}

// ─── Tenant (Owner manages) DTOs ─────────────────────────────────────────────

export interface TenantRecord {
  occupantID:       number;
  fullName:         string;
  identificationNo: string;
  email:            string;
  contactNumber:    string;
  contractID:       number;
  unitNumber:       string;
  startDate:        string;
  endDate?:         string;
  status:           string;   // 'Active' | 'PendingApproval' | 'Expired' | 'Terminated'
  documentPath?:    string;
}

export interface AddTenantDto {
  fullName:         string;
  identificationNo: string;
  email:            string;
  contactNumber:    string;
  gender:           string;
  unitID:           number;
  startDate:        string;
  endDate:          string;
  agreementFileRef: string;   // Temp file reference from upload service
}

export interface TenantRemovalDto {
  contractID:      number;
  removalType:     'Auto' | 'EarlyTermination';
  reason?:         string;
}

// ─── Staff Management DTOs ────────────────────────────────────────────────────

export interface StaffRecord {
  accountID:        number;
  fullName:         string;
  email:            string;
  roleType:         string;   // 'Technician' | 'PropertyManager'
  accountStatus:    string;
  lastLogin?:       string;
  // Technician-specific
  technicianID?:    number;
  serviceTypeName?: string;
  experienceLevel?: string;
  availabilityStatus?: string;
  ranking?:         number;
  // Manager-specific
  managerID?:       number;
  position?:        string;
}

export interface StaffDeactivateDto {
  accountID:    number;
  reasonCode:   string;   // 'Resigned' | 'Terminated' | 'OnLeave' | 'Other'
  reasonDetail?: string;
}

/** Returned by POST /api/Upload/temp — two-phase document upload */
export interface TempUploadResult {
  fileRef:    string;   // Temporary reference to the uploaded file (UUID)
  fileName:   string;
  expiresAt:  string;   // ISO timestamp — frontend warns if user takes too long
}

// ─── Profile Update DTOs ─────────────────────────────────────────────────────

export interface UpdateProfileDto {
  fullName:       string;
  contactNumber:  string;
  gender:         string;
}

export interface ChangePasswordDto {
  email?:          string;
  currentPassword: string;
  newPassword:     string;
  confirmPassword: string;
}


