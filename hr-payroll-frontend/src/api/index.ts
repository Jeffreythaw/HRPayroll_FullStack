import axios from 'axios';
import type {
  LoginRequest, LoginResponse,
  Department, CreateDepartmentRequest,
  Employee, CreateEmployeeRequest, UpdateEmployeeRequest,
  EmployeePayrollProfile, CreateEmployeePayrollProfileRequest, UpdateEmployeePayrollProfileRequest,
  Attendance, CreateAttendanceRequest, UpdateAttendanceRequest, AttendanceSummary,
  AttendanceLookup, CreateAttendanceLookupRequest, UpdateAttendanceLookupRequest,
  PayrollRecord, ProcessPayrollRequest, UpdatePayrollStatusRequest,
  DashboardSummary, ExcelReportRequest,
} from '../types';

const apiBaseURL = import.meta.env.VITE_API_BASE_URL ?? '/api';

const api = axios.create({
  baseURL: apiBaseURL,
  headers: { 'Content-Type': 'application/json' },
});

async function saveBlob(blob: Blob, filename: string) {
  const anyWindow = window as Window & {
    showSaveFilePicker?: (options: {
      suggestedName?: string;
      types?: Array<{
        description?: string;
        accept: Record<string, string[]>;
      }>;
    }) => Promise<FileSystemFileHandle>;
  };

  if (typeof anyWindow.showSaveFilePicker === 'function') {
    const handle = await anyWindow.showSaveFilePicker({
      suggestedName: filename,
      types: [
        {
          description: filename.endsWith('.pdf') ? 'PDF document' : 'Spreadsheet',
          accept: {
            [filename.endsWith('.pdf') ? 'application/pdf' : 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet']:
              [filename.split('.').pop() === 'pdf' ? '.pdf' : '.xlsx'],
          },
        },
      ],
    });

    const writable = await handle.createWritable();
    await writable.write(blob);
    await writable.close();
    return;
  }

  const url = window.URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
}

// ── Request interceptor: attach JWT ───────────────────────────
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// ── Response interceptor: handle 401 ─────────────────────────
api.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err.response?.status === 401) {
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      window.location.href = '/login';
    }
    return Promise.reject(err);
  }
);

// ─── Auth ─────────────────────────────────────────────────────
export const authApi = {
  login: (data: LoginRequest) =>
    api.post<LoginResponse>('/auth/login', data).then(r => r.data),
};

// ─── Dashboard ────────────────────────────────────────────────
export const dashboardApi = {
  getSummary: () => api.get<DashboardSummary>('/dashboard').then(r => r.data),
};

// ─── Departments ──────────────────────────────────────────────
export const departmentApi = {
  getAll: () => api.get<Department[]>('/departments').then(r => r.data),
  getById: (id: number) => api.get<Department>(`/departments/${id}`).then(r => r.data),
  create: (data: CreateDepartmentRequest) => api.post<Department>('/departments', data).then(r => r.data),
  update: (id: number, data: CreateDepartmentRequest) => api.put<Department>(`/departments/${id}`, data).then(r => r.data),
  delete: (id: number) => api.delete(`/departments/${id}`),
};

// ─── Employees ────────────────────────────────────────────────
export const employeeApi = {
  getAll: (params?: { status?: string; departmentId?: number }) =>
    api.get<Employee[]>('/employees', { params }).then(r => r.data),
  getById: (id: number) => api.get<Employee>(`/employees/${id}`).then(r => r.data),
  create: (data: CreateEmployeeRequest) => api.post<Employee>('/employees', data).then(r => r.data),
  update: (id: number, data: UpdateEmployeeRequest) => api.put<Employee>(`/employees/${id}`, data).then(r => r.data),
  delete: (id: number) => api.delete(`/employees/${id}`),
};

// ─── Employee Payroll Profiles ───────────────────────────────
export const employeeProfileApi = {
  getAll: (params?: { employeeId?: number; status?: string }) =>
    api.get<EmployeePayrollProfile[]>('/employee-profiles', { params }).then(r => r.data),
  getById: (id: number) => api.get<EmployeePayrollProfile>(`/employee-profiles/${id}`).then(r => r.data),
  create: (data: CreateEmployeePayrollProfileRequest) =>
    api.post<EmployeePayrollProfile>('/employee-profiles', data).then(r => r.data),
  update: (id: number, data: UpdateEmployeePayrollProfileRequest) =>
    api.put<EmployeePayrollProfile>(`/employee-profiles/${id}`, data).then(r => r.data),
  delete: (id: number) => api.delete(`/employee-profiles/${id}`),
};

// ─── Attendance ───────────────────────────────────────────────
export const attendanceApi = {
  getByMonth: (month: number, year: number, employeeId?: number) =>
    api.get<Attendance[]>('/attendances', { params: { month, year, employeeId } }).then(r => r.data),
  getById: (id: number) => api.get<Attendance>(`/attendances/${id}`).then(r => r.data),
  getSummary: (employeeId: number, month: number, year: number) =>
    api.get<AttendanceSummary>('/attendances/summary', { params: { employeeId, month, year } }).then(r => r.data),
  create: (data: CreateAttendanceRequest) => api.post<Attendance>('/attendances', data).then(r => r.data),
  update: (id: number, data: UpdateAttendanceRequest) => api.put<Attendance>(`/attendances/${id}`, data).then(r => r.data),
  delete: (id: number) => api.delete(`/attendances/${id}`),
};

// ─── Attendance Lookups ───────────────────────────────────────
export const attendanceLookupApi = {
  getAll: (category?: string) =>
    api.get<AttendanceLookup[]>('/attendance-lookups', { params: { category } }).then(r => r.data),
  getById: (id: number) => api.get<AttendanceLookup>(`/attendance-lookups/${id}`).then(r => r.data),
  create: (data: CreateAttendanceLookupRequest) => api.post<AttendanceLookup>('/attendance-lookups', data).then(r => r.data),
  update: (id: number, data: UpdateAttendanceLookupRequest) => api.put<AttendanceLookup>(`/attendance-lookups/${id}`, data).then(r => r.data),
  delete: (id: number) => api.delete(`/attendance-lookups/${id}`),
};

// ─── Payroll ──────────────────────────────────────────────────
export const payrollApi = {
  getByMonth: (month: number, year: number) =>
    api.get<PayrollRecord[]>('/payroll', { params: { month, year } }).then(r => r.data),
  getById: (id: number) => api.get<PayrollRecord>(`/payroll/${id}`).then(r => r.data),
  process: (data: ProcessPayrollRequest) => api.post<PayrollRecord[]>('/payroll/process', data).then(r => r.data),
  updateStatus: (id: number, data: UpdatePayrollStatusRequest) =>
    api.patch<PayrollRecord>(`/payroll/${id}/status`, data).then(r => r.data),
  delete: (id: number) => api.delete(`/payroll/${id}`),
};

// ─── Reports ──────────────────────────────────────────────────
export const reportApi = {
  generateExcel: async (data: ExcelReportRequest) => {
    const response = await api.post('/reports/excel', data, { responseType: 'blob' });
    const monthName = new Date(data.year, data.month - 1).toLocaleString('default', { month: 'long' });
    await saveBlob(new Blob([response.data]), `Payroll_Report_${monthName}_${data.year}.xlsx`);
  },
  generatePaymentVoucher: async (data: ExcelReportRequest) => {
    const response = await api.post('/reports/payment-voucher', data, { responseType: 'blob' });
    const monthName = new Date(data.year, data.month - 1).toLocaleString('default', { month: 'short' });
    await saveBlob(new Blob([response.data]), `Payment_Voucher_${monthName}_${data.year}.pdf`);
  },
};

export default api;
