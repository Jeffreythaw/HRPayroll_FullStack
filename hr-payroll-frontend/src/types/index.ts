// ─── Auth ─────────────────────────────────────────────────────
export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  username: string;
  role: string;
  expiresAt: string;
}

// ─── Department ───────────────────────────────────────────────
export interface Department {
  id: number;
  name: string;
  description?: string;
  employeeCount: number;
}

export interface CreateDepartmentRequest {
  name: string;
  description?: string;
}

// ─── Employee ─────────────────────────────────────────────────
export interface Employee {
  id: number;
  employeeCode: string;
  finNo?: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phone?: string;
  departmentId: number;
  departmentName: string;
  position: string;
  salaryMode: string;
  basicSalary: number;
  dailyRate: number;
  shiftAllowance: number;
  otRatePerHour: number;
  sundayPhOtDays: number;
  publicHolidayOtHours: number;
  transportationFee: number;
  deductionNoWork4Days: number;
  advanceSalary: number;
  standardWorkHours: number;
  joinDate?: string | null;
  status: string;
}

export interface CreateEmployeeRequest {
  finNo?: string;
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  departmentId: number;
  position: string;
  salaryMode: string;
  basicSalary: number;
  dailyRate: number;
  shiftAllowance: number;
  otRatePerHour: number;
  sundayPhOtDays: number;
  publicHolidayOtHours: number;
  transportationFee: number;
  standardWorkHours: number;
  joinDate?: string | null;
}

export interface UpdateEmployeeRequest extends CreateEmployeeRequest {
  status: string;
}

// ─── Employee Payroll Profiles ───────────────────────────────
export interface EmployeePayrollProfile {
  id: number;
  employeeId: number;
  employeeCode: string;
  employeeName: string;
  profileName: string;
  salaryMode: string;
  basicSalary: number;
  dailyRate: number;
  shiftAllowance: number;
  otRatePerHour: number;
  sundayPhOtDays: number;
  publicHolidayOtHours: number;
  transportationFee: number;
  deductionNoWork4Days: number;
  advanceSalary: number;
  standardWorkHours: number;
  isPrimary: boolean;
  status: string;
}

export interface CreateEmployeePayrollProfileRequest {
  employeeId: number;
  profileName: string;
  salaryMode: string;
  basicSalary: number;
  dailyRate: number;
  shiftAllowance: number;
  otRatePerHour: number;
  sundayPhOtDays: number;
  publicHolidayOtHours: number;
  transportationFee: number;
  standardWorkHours: number;
  isPrimary: boolean;
  status: string;
}

export interface UpdateEmployeePayrollProfileRequest extends CreateEmployeePayrollProfileRequest {}

// ─── Attendance ───────────────────────────────────────────────
export interface Attendance {
  id: number;
  employeeId: number;
  employeeName: string;
  employeeCode: string;
  date: string;
  start?: string;
  end?: string;
  isOvernight: boolean;
  workHours: number;
  otHours: number;
  siteProject?: string;
  transport?: string;
  status: string;
  remarks?: string;
}

export interface CreateAttendanceRequest {
  employeeId: number;
  date: string;
  start?: string;
  end?: string;
  isOvernight: boolean;
  siteProject?: string;
  transport?: string;
  status: string;
  remarks?: string;
}

export interface UpdateAttendanceRequest {
  start?: string;
  end?: string;
  isOvernight: boolean;
  siteProject?: string;
  transport?: string;
  status: string;
  remarks?: string;
}

export interface AttendanceLookup {
  id: number;
  category: string;
  name: string;
  isActive: boolean;
  sortOrder: number;
}

export interface CreateAttendanceLookupRequest {
  category: string;
  name: string;
  isActive: boolean;
  sortOrder: number;
}

export interface UpdateAttendanceLookupRequest extends CreateAttendanceLookupRequest {}

// ─── Public Holidays ─────────────────────────────────────────
export interface PublicHoliday {
  id: number;
  date: string;
  name: string;
  year: number;
  countryCode: string;
  source: string;
}

export interface AttendanceSummary {
  employeeId: number;
  employeeName: string;
  month: number;
  year: number;
  workingDays: number;
  presentDays: number;
  absentDays: number;
  leaveDays: number;
  totalWorkHours: number;
  totalOTHours: number;
}

// ─── Payroll ──────────────────────────────────────────────────
export interface PayrollRecord {
  id: number;
  employeeId: number;
  employeeName: string;
  employeeCode: string;
  departmentName: string;
  position: string;
  payrollProfileName: string;
  salaryMode: string;
  month: number;
  year: number;
  workingDays: number;
  presentDays: number;
  absentDays: number;
  leaveDays: number;
  totalWorkHours: number;
  totalOTHours: number;
  basicSalary: number;
  dailyRate: number;
  otAmount: number;
  deductions: number;
  grossSalary: number;
  netSalary: number;
  status: string;
  notes?: string;
  processedAt: string;
}

export interface ProcessPayrollRequest {
  month: number;
  year: number;
  employeeIds?: number[];
  adjustments?: PayrollProcessAdjustment[];
}

export interface PayrollProcessAdjustment {
  employeePayrollProfileId: number;
  advanceSalary: number;
  deductionNoWork4Days: number;
}

export interface UpdatePayrollStatusRequest {
  status: string;
  notes?: string;
}

// ─── Dashboard ────────────────────────────────────────────────
export interface DashboardSummary {
  totalEmployees: number;
  activeEmployees: number;
  presentToday: number;
  absentToday: number;
  onLeaveToday: number;
  totalPayrollThisMonth: number;
  departmentHeadcounts: DepartmentHeadcount[];
  recentAttendances: RecentAttendance[];
}

export interface DepartmentHeadcount {
  department: string;
  count: number;
}

export interface RecentAttendance {
  employeeName: string;
  status: string;
  start?: string;
  date: string;
}

// ─── Excel Report ─────────────────────────────────────────────
export interface ExcelReportRequest {
  month: number;
  year: number;
  employeeIds?: number[];
  profileIds?: number[];
}
