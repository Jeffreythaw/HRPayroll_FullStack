import { useEffect, useState, useCallback } from 'react';
import { useForm } from 'react-hook-form';
import toast from 'react-hot-toast';
import { employeeApi, departmentApi, employeeProfileApi } from '../../api';
import {
  Modal, ConfirmModal, PageHeader, EmptyState, Spinner, FormField
} from '../../components/ui';
import { formatCurrency, formatDate, statusBadgeClass } from '../../utils';
import type { Employee, CreateEmployeeRequest, UpdateEmployeeRequest, Department } from '../../types';

type EmployeeFormValues = {
  name: string;
  finNo?: string;
  phone?: string;
  bank?: string;
  departmentId: number;
  joinDate?: string;
  salaryMode: string;
  basicSalary: number;
  dailyRate: number;
  shiftAllowance: number;
  transportationFee: number;
  status: string;
};

export default function EmployeesPage() {
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [filterStatus, setFilterStatus] = useState('');
  const [filterDept, setFilterDept] = useState('');

  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<Employee | null>(null);
  const [profileTarget, setProfileTarget] = useState<Employee | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<Employee | null>(null);
  const [deleting, setDeleting] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [emps, depts] = await Promise.all([
        employeeApi.getAll(),
        departmentApi.getAll(),
      ]);
      setEmployees(emps);
      setDepartments(depts);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const filtered = employees.filter(e => {
    const matchSearch = !search ||
      e.fullName.toLowerCase().includes(search.toLowerCase()) ||
      e.employeeCode.toLowerCase().includes(search.toLowerCase()) ||
      e.email.toLowerCase().includes(search.toLowerCase());
    const matchStatus = !filterStatus || e.status === filterStatus;
    const matchDept = !filterDept || String(e.departmentId) === filterDept;
    return matchSearch && matchStatus && matchDept;
  });

  const openCreate = () => { setEditing(null); setShowForm(true); };
  const openEdit = (e: Employee) => { setEditing(e); setShowForm(true); };
  const openProfiles = (e: Employee) => setProfileTarget(e);
  const closeForm = () => { setShowForm(false); setEditing(null); };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    try {
      await employeeApi.delete(deleteTarget.id);
      toast.success('Employee deactivated');
      setDeleteTarget(null);
      load();
    } catch { toast.error('Failed to deactivate employee'); }
    finally { setDeleting(false); }
  };

  return (
    <div>
      <PageHeader
        title="Employees"
        subtitle={`${employees.filter(e => e.status === 'Active').length} active employees`}
        actions={
          <button className="btn-primary" onClick={openCreate}>
            <PlusIcon /> Add Employee
          </button>
        }
      />

      {/* Filters */}
      <div className="card p-4 mb-4 grid grid-cols-1 gap-3 md:grid-cols-2 xl:flex xl:flex-wrap xl:items-center">
        <input
          value={search}
          onChange={e => setSearch(e.target.value)}
          className="input w-full md:w-auto md:min-w-56"
          placeholder="Search name, code, email…"
        />
        <select value={filterStatus} onChange={e => setFilterStatus(e.target.value)} className="input w-full md:w-auto md:min-w-36">
          <option value="">All Status</option>
          <option value="Active">Active</option>
          <option value="Inactive">Inactive</option>
        </select>
        <select value={filterDept} onChange={e => setFilterDept(e.target.value)} className="input w-full md:w-auto md:min-w-48">
          <option value="">All Departments</option>
          {departments.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
        </select>
        <span className="text-sm text-slate-500 xl:ml-auto">{filtered.length} result{filtered.length !== 1 ? 's' : ''}</span>
      </div>

      {/* List */}
      <div className="card overflow-hidden">
        {loading ? (
          <div className="flex justify-center py-16"><Spinner size="lg" /></div>
        ) : filtered.length === 0 ? (
          <EmptyState message="No employees found" />
        ) : (
          <>
            <div className="lg:hidden divide-y divide-slate-100">
              {filtered.map(emp => (
                <div key={emp.id} className="p-4 space-y-4">
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex items-center gap-3 min-w-0">
                      <div className="w-10 h-10 bg-navy-100 text-navy-800 rounded-full flex items-center justify-center text-xs font-bold flex-shrink-0">
                        {emp.firstName[0]}{emp.lastName[0]}
                      </div>
                      <div className="min-w-0">
                        <p className="font-semibold text-slate-900 leading-tight truncate">{emp.fullName}</p>
                        <p className="text-xs text-slate-400 font-mono">{emp.employeeCode}</p>
                        <p className="text-xs text-slate-500 mt-1 truncate">{emp.departmentName}</p>
                      </div>
                    </div>
                    <span className={statusBadgeClass(emp.status)}>{emp.status}</span>
                  </div>

                  <div className="grid grid-cols-2 gap-3">
                    <MiniInfo label="Position" value={emp.position || '—'} />
                    <MiniInfo label="Join Date" value={formatDate(emp.joinDate) || '—'} />
                    <MiniInfo label="Basic Salary" value={formatCurrency(emp.basicSalary)} />
                    <MiniInfo label="OT Rate/Hr" value={formatCurrency(emp.otRatePerHour)} accent="text-indigo-600" />
                  </div>

                  <div className="flex flex-wrap items-center gap-2">
                    <button
                      onClick={() => openProfiles(emp)}
                      className="btn-secondary px-3 py-2 text-xs"
                    >
                      Manage Profiles
                    </button>
                    <button
                      onClick={() => openEdit(emp)}
                      className="btn-primary px-3 py-2 text-xs"
                    >
                      Edit
                    </button>
                    <button
                      onClick={() => setDeleteTarget(emp)}
                      className="btn-danger px-3 py-2 text-xs"
                      disabled={emp.status === 'Inactive'}
                    >
                      Deactivate
                    </button>
                  </div>
                </div>
              ))}
            </div>

            <div className="hidden lg:block overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr>
                    <th className="table-th">Employee</th>
                    <th className="table-th">Department</th>
                    <th className="table-th">Position</th>
                    <th className="table-th">Basic Salary</th>
                    <th className="table-th">OT Rate/Hr</th>
                    <th className="table-th">Join Date</th>
                    <th className="table-th">Profiles</th>
                    <th className="table-th">Status</th>
                    <th className="table-th w-24">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {filtered.map(emp => (
                    <tr key={emp.id} className="hover:bg-slate-50 transition-colors">
                      <td className="table-td">
                        <div className="flex items-center gap-3">
                          <div className="w-8 h-8 bg-navy-100 text-navy-800 rounded-full flex items-center justify-center text-xs font-bold flex-shrink-0">
                            {emp.firstName[0]}{emp.lastName[0]}
                          </div>
                          <div>
                            <p className="font-semibold text-slate-900 leading-tight">{emp.fullName}</p>
                            <p className="text-xs text-slate-400 font-mono">{emp.employeeCode}</p>
                          </div>
                        </div>
                      </td>
                      <td className="table-td text-slate-600">{emp.departmentName}</td>
                      <td className="table-td text-slate-600">{emp.position}</td>
                      <td className="table-td font-mono font-medium">{formatCurrency(emp.basicSalary)}</td>
                      <td className="table-td font-mono">{formatCurrency(emp.otRatePerHour)}</td>
                      <td className="table-td text-slate-500">{formatDate(emp.joinDate)}</td>
                      <td className="table-td">
                        <button
                          onClick={() => openProfiles(emp)}
                          className="text-xs font-semibold text-navy-800 hover:underline"
                        >
                          Manage Profiles
                        </button>
                      </td>
                      <td className="table-td">
                        <span className={statusBadgeClass(emp.status)}>{emp.status}</span>
                      </td>
                      <td className="table-td">
                        <div className="flex items-center gap-1">
                          <button
                            onClick={() => openEdit(emp)}
                            className="p-1.5 text-slate-400 hover:text-navy-800 hover:bg-navy-50 rounded transition-colors"
                            title="Edit"
                          >
                            <EditIcon />
                          </button>
                          <button
                            onClick={() => setDeleteTarget(emp)}
                            className="p-1.5 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded transition-colors"
                            title="Deactivate"
                            disabled={emp.status === 'Inactive'}
                          >
                            <TrashIcon />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </>
        )}
      </div>

      {/* Form modal */}
      <EmployeeFormModal
        open={showForm}
        onClose={closeForm}
        employee={editing}
        departments={departments}
        onSaved={() => { closeForm(); load(); }}
      />

      <EmployeeProfilesModal
        open={!!profileTarget}
        employee={profileTarget}
        onClose={() => setProfileTarget(null)}
      />

      {/* Delete confirm */}
      <ConfirmModal
        open={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        onConfirm={handleDelete}
        title="Deactivate Employee"
        message={`Are you sure you want to deactivate ${deleteTarget?.fullName}? This will mark them as inactive.`}
        confirmLabel="Deactivate"
        loading={deleting}
      />
    </div>
  );
}

// ─── Employee Form Modal ──────────────────────────────────────
interface EmployeeFormModalProps {
  open: boolean;
  onClose: () => void;
  employee: Employee | null;
  departments: Department[];
  onSaved: () => void;
}

function EmployeeFormModal({ open, onClose, employee, departments, onSaved }: EmployeeFormModalProps) {
  const isEdit = !!employee;
  const [saving, setSaving] = useState(false);

  const { register, handleSubmit, reset, watch, formState: { errors } } = useForm<EmployeeFormValues>({
    defaultValues: {
      name: '',
      finNo: '',
      joinDate: '',
      salaryMode: 'Monthly',
      basicSalary: 0,
      dailyRate: 0,
      shiftAllowance: 0,
      transportationFee: 0,
      departmentId: 0,
      status: 'Active',
    }
  });
  const salaryMode = watch('salaryMode') || 'Monthly';
  const basicSalary = watch('basicSalary') || 0;
  const dailyRate = watch('dailyRate') || 0;
  const autoOtRate = Math.max(0, salaryMode === 'Daily'
    ? (dailyRate / 8) * 1.5
    : (basicSalary / 24 / 11) * 1.5);

  useEffect(() => {
    if (employee) {
      reset({
        name: employee.fullName.trim(),
        finNo: employee.finNo || '',
        phone: employee.phone,
        bank: employee.bank,
        departmentId: employee.departmentId,
        salaryMode: employee.salaryMode || (employee.dailyRate > 0 ? 'Daily' : 'Monthly'),
        basicSalary: employee.basicSalary,
        dailyRate: employee.dailyRate,
        shiftAllowance: employee.shiftAllowance,
        transportationFee: employee.transportationFee,
        joinDate: employee.joinDate || '',
        status: employee.status,
      });
    } else {
      reset({
        name: '',
        finNo: '',
        phone: '',
        bank: '',
        departmentId: 0,
        salaryMode: 'Monthly',
        basicSalary: 0,
        dailyRate: 0,
        shiftAllowance: 0,
        transportationFee: 0,
        status: 'Active',
        joinDate: '',
      });
    }
  }, [employee, reset]);

  const splitName = (value: string) => {
    const trimmed = value.trim();
    if (!trimmed) return { firstName: '', lastName: '' };
    const parts = trimmed.split(/\s+/);
    if (parts.length === 1) return { firstName: trimmed, lastName: '' };
    return {
      firstName: parts.slice(0, -1).join(' '),
      lastName: parts[parts.length - 1],
    };
  };

  const onSubmit = async (data: EmployeeFormValues) => {
    setSaving(true);
    try {
      const { firstName, lastName } = splitName(data.name);
      const derivedEmail = `${(data.finNo || data.name || 'employee')
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, '.')
        .replace(/^\.+|\.+$/g, '')}@company.local`;
      const payload: CreateEmployeeRequest = {
        finNo: data.finNo?.trim() || undefined,
        firstName,
        lastName,
        email: isEdit && employee ? employee.email : derivedEmail,
        phone: data.phone,
        bank: data.bank,
        departmentId: data.departmentId,
        position: isEdit && employee ? employee.position : 'Employee',
        salaryMode: data.salaryMode,
        basicSalary: data.basicSalary,
        dailyRate: data.dailyRate,
        shiftAllowance: data.shiftAllowance,
        otRatePerHour: autoOtRate,
        transportationFee: data.transportationFee,
        standardWorkHours: 8,
        joinDate: data.joinDate || null,
      };

      if (isEdit && employee) {
        await employeeApi.update(employee.id, {
          ...payload,
          status: data.status,
        } as UpdateEmployeeRequest);
        toast.success('Employee updated');
      } else {
        await employeeApi.create(payload);
        toast.success('Employee created');
      }
      onSaved();
    } catch (err: any) {
      toast.error(err?.response?.data?.message || 'Failed to save employee');
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={isEdit ? 'Edit Employee' : 'Add Employee'}
      size="lg"
      footer={
        <>
          <button className="btn-secondary" onClick={onClose} disabled={saving}>Cancel</button>
          <button className="btn-primary" onClick={handleSubmit(onSubmit)} disabled={saving}>
            {saving ? <><Spinner size="sm" /> Saving…</> : (isEdit ? 'Save Changes' : 'Create Employee')}
          </button>
        </>
      }
      >
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="space-y-4">
          <div>
            <h3 className="text-sm font-semibold text-slate-700 mb-3">Personal Details</h3>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <FormField label="Name" error={errors.name?.message} required>
                <input {...register('name', { required: 'Required' })} className="input" placeholder="John Tan" />
              </FormField>
              <FormField label="Fin No" error={errors.finNo?.message} required>
                <input {...register('finNo', { required: 'Required' })} className="input" placeholder="F1234567N" />
              </FormField>
              <FormField label="Phone">
                <input {...register('phone')} className="input" placeholder="+65 9123 4567" />
              </FormField>
              <FormField label="Bank">
                <input {...register('bank')} className="input" placeholder="POSB / DBS / OCBC" />
              </FormField>
              <FormField label="Department" error={errors.departmentId?.message} required>
                <select {...register('departmentId', { required: 'Required', valueAsNumber: true })} className="input">
                  <option value="">Select department</option>
                  {departments.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
                </select>
              </FormField>
              <FormField label="Join Date">
                <input {...register('joinDate')} type="date" className="input" />
              </FormField>
            </div>
          </div>

        <div>
          <h3 className="text-sm font-semibold text-slate-700 mb-3">Salary & Allowances</h3>
          <p className="mb-3 text-xs text-slate-500">
            Keep this section to stable pay setup. Sunday / public holiday premiums are calculated from attendance and the Singapore public holiday calendar.
          </p>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <FormField label="Salary Mode" required>
              <select {...register('salaryMode')} className="input">
                  <option value="Monthly">Monthly</option>
                  <option value="Daily">Daily</option>
                </select>
              </FormField>
              <FormField
                label={salaryMode === 'Daily' ? 'Monthly Salary BASIC S$ (leave 0 for daily staff)' : 'Monthly Salary BASIC S$'}
                error={errors.basicSalary?.message}
                required={salaryMode === 'Monthly'}
              >
                <input
                  {...register('basicSalary', {
                    required: salaryMode === 'Monthly' ? 'Required' : false,
                    valueAsNumber: true,
                    min: { value: 0, message: 'Must be positive' }
                  })}
                  type="number"
                  step="0.01"
                  className="input"
                  placeholder="3000.00"
                />
              </FormField>
              <FormField label="Daily Rate S$" error={errors.dailyRate?.message} required={salaryMode === 'Daily'}>
                <input
                  {...register('dailyRate', {
                    required: salaryMode === 'Daily' ? 'Required' : false,
                    valueAsNumber: true,
                    min: { value: 0, message: 'Must be positive' }
                  })}
                  type="number"
                  step="0.01"
                  className="input"
                  placeholder={salaryMode === 'Daily' ? '24.00' : '0.00'}
                />
              </FormField>
              <FormField label="Shift Allowance S$">
                <input {...register('shiftAllowance', { valueAsNumber: true })} type="number" step="0.01" className="input" placeholder="0.00" />
              </FormField>
              <FormField label="Transportation Fee">
                <input {...register('transportationFee', { valueAsNumber: true })} type="number" step="0.01" className="input" placeholder="0.00" />
              </FormField>
              <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">OT Rate / Hour</p>
                <p className="mt-2 text-lg font-semibold text-slate-900">{formatCurrency(autoOtRate)}</p>
                <p className="mt-1 text-xs text-slate-500">
                  Auto: {salaryMode === 'Daily' ? 'daily rate / 8 x 1.5' : 'basic / 24 / 11 x 1.5'}
                </p>
              </div>
            </div>
          </div>
        </div>

        <div className="space-y-4">
          {isEdit && (
            <div>
              <h3 className="text-sm font-semibold text-slate-700 mb-3">Status</h3>
              <FormField label="Status">
                <select {...register('status')} className="input">
                  <option value="Active">Active</option>
                  <option value="Inactive">Inactive</option>
                </select>
              </FormField>
            </div>
          )}
        </div>
      </div>
    </Modal>
  );
}

// ─── Employee Payroll Profiles Modal ─────────────────────────
type ProfileFormValues = {
  profileName: string;
  salaryMode: string;
  basicSalary: number;
  dailyRate: number;
  shiftAllowance: number;
  otRatePerHour: number;
  transportationFee: number;
  standardWorkHours: number;
  status: string;
};

function EmployeeProfilesModal({ open, onClose, employee }: { open: boolean; onClose: () => void; employee: Employee | null; }) {
  const [profiles, setProfiles] = useState<import('../../types').EmployeePayrollProfile[]>([]);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [editingProfile, setEditingProfile] = useState<import('../../types').EmployeePayrollProfile | null>(null);

  const { register, handleSubmit, reset, watch, formState: { errors } } = useForm<ProfileFormValues>({
    defaultValues: {
      profileName: '',
      salaryMode: 'Monthly',
      basicSalary: 0,
      dailyRate: 0,
      shiftAllowance: 0,
      otRatePerHour: 0,
      transportationFee: 0,
      standardWorkHours: 8,
      status: 'Active',
    }
  });

  const salaryMode = watch('salaryMode') || 'Monthly';
  const basicSalary = watch('basicSalary') || 0;
  const dailyRate = watch('dailyRate') || 0;
  const autoOtRate = Math.max(0, salaryMode === 'Daily'
    ? (dailyRate / 8) * 1.5
    : (basicSalary / 24 / 11) * 1.5);

  const loadProfiles = useCallback(async () => {
    if (!employee) return;
    setLoading(true);
    try {
      const data = await employeeProfileApi.getAll({ employeeId: employee.id });
      setProfiles(data);
    } finally {
      setLoading(false);
    }
  }, [employee]);

  useEffect(() => {
    if (open && employee) {
      loadProfiles();
      setEditingProfile(null);
      reset({
        profileName: '',
        salaryMode: 'Monthly',
        basicSalary: 0,
        dailyRate: 0,
        shiftAllowance: 0,
        otRatePerHour: 0,
        transportationFee: 0,
        standardWorkHours: 8,
        status: 'Active',
      });
    }
  }, [open, employee, loadProfiles, reset]);

  const startEdit = (profile: import('../../types').EmployeePayrollProfile) => {
    setEditingProfile(profile);
    reset({
      profileName: profile.profileName,
      salaryMode: profile.salaryMode,
      basicSalary: profile.basicSalary,
      dailyRate: profile.dailyRate,
      shiftAllowance: profile.shiftAllowance,
      otRatePerHour: profile.otRatePerHour,
      transportationFee: profile.transportationFee,
      standardWorkHours: profile.standardWorkHours,
      status: profile.status,
    });
  };

  const resetForm = () => {
    setEditingProfile(null);
    reset({
      profileName: '',
      salaryMode: 'Monthly',
      basicSalary: 0,
      dailyRate: 0,
      shiftAllowance: 0,
      otRatePerHour: 0,
      transportationFee: 0,
      standardWorkHours: 8,
      status: 'Active',
    });
  };

  const onSubmit = async (data: ProfileFormValues) => {
    if (!employee) return;
    setSaving(true);
    try {
      const payload = {
        employeeId: employee.id,
        profileName: data.profileName.trim(),
        salaryMode: data.salaryMode,
        basicSalary: data.basicSalary,
        dailyRate: data.dailyRate,
        shiftAllowance: data.shiftAllowance,
        otRatePerHour: data.otRatePerHour > 0 ? data.otRatePerHour : autoOtRate,
        transportationFee: data.transportationFee,
        standardWorkHours: data.standardWorkHours,
        isPrimary: false,
        status: data.status,
      };

      if (editingProfile) {
        await employeeProfileApi.update(editingProfile.id, payload);
        toast.success('Profile updated');
      } else {
        await employeeProfileApi.create(payload);
        toast.success('Profile added');
      }
      await loadProfiles();
      resetForm();
    } catch (err: any) {
      toast.error(err?.response?.data?.message || 'Failed to save profile');
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (profileId: number) => {
    if (!confirm('Delete this secondary profile?')) return;
    try {
      await employeeProfileApi.delete(profileId);
      toast.success('Profile deleted');
      await loadProfiles();
    } catch (err: any) {
      toast.error(err?.response?.data?.message || 'Failed to delete profile');
    }
  };

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={employee ? `Payroll Profiles - ${employee.fullName}` : 'Payroll Profiles'}
      size="lg"
      footer={
        <>
          <button className="btn-secondary" onClick={onClose}>Close</button>
          <button className="btn-primary" onClick={handleSubmit(onSubmit)} disabled={!employee || saving}>
            {saving ? <><Spinner size="sm" /> Saving…</> : (editingProfile ? 'Save Profile' : 'Add Profile')}
          </button>
        </>
      }
    >
      <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
        <div className="space-y-4">
          <div>
            <h3 className="text-sm font-semibold text-slate-700 mb-3">Current Profiles</h3>
            {loading ? (
              <div className="flex justify-center py-8"><Spinner /></div>
            ) : (
              <div className="space-y-3">
                {profiles.map(profile => (
                  <div key={profile.id} className="rounded-2xl border border-slate-200 p-4 bg-white">
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <div className="flex items-center gap-2">
                          <p className="font-semibold text-slate-900">{profile.profileName}</p>
                          {profile.isPrimary && <span className="text-[10px] px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-700 font-semibold">Primary</span>}
                        </div>
                        <p className="text-xs text-slate-500 mt-1">
                          {profile.salaryMode} · {formatCurrency(profile.basicSalary || profile.dailyRate)} · OT {formatCurrency(profile.otRatePerHour)}
                        </p>
                        <p className="text-xs text-slate-400 mt-1">
                          Shift {formatCurrency(profile.shiftAllowance)} · Transport {formatCurrency(profile.transportationFee)} · Status {profile.status}
                        </p>
                      </div>
                      <div className="flex items-center gap-2">
                        {!profile.isPrimary && (
                          <>
                            <button
                              className="text-xs font-semibold text-navy-800 hover:underline"
                              onClick={() => startEdit(profile)}
                            >
                              Edit
                            </button>
                            <button
                              className="text-xs font-semibold text-red-600 hover:underline"
                              onClick={() => handleDelete(profile.id)}
                            >
                              Delete
                            </button>
                          </>
                        )}
                        {profile.isPrimary && <span className="text-xs text-slate-400">Synced from employee form</span>}
                      </div>
                    </div>
                  </div>
                ))}
                {profiles.length === 0 && (
                  <EmptyState message="No payroll profiles yet" />
                )}
              </div>
            )}
          </div>
        </div>

        <div className="space-y-4">
          <div>
            <h3 className="text-sm font-semibold text-slate-700 mb-3">{editingProfile ? 'Edit Secondary Profile' : 'Add Secondary Profile'}</h3>
            <p className="mb-3 text-xs text-slate-500">
              Use this for fixed salary structure only. Sunday / public holiday premium is derived from attendance and public holiday data during payroll processing.
            </p>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <FormField label="Profile Name" error={errors.profileName?.message} required>
                <input {...register('profileName', { required: 'Required' })} className="input" placeholder="Transport Included" />
              </FormField>
              <FormField label="Salary Mode" required>
                <select {...register('salaryMode')} className="input">
                  <option value="Monthly">Monthly</option>
                  <option value="Daily">Daily</option>
                </select>
              </FormField>
              <FormField label="Monthly Salary BASIC S$" required={salaryMode === 'Monthly'}>
                <input {...register('basicSalary', { valueAsNumber: true })} type="number" step="0.01" className="input" placeholder="0.00" />
              </FormField>
              <FormField label="Daily Rate S$" required={salaryMode === 'Daily'}>
                <input {...register('dailyRate', { valueAsNumber: true })} type="number" step="0.01" className="input" placeholder={salaryMode === 'Daily' ? '24.00' : '0.00'} />
              </FormField>
              <FormField label="Shift Allowance S$">
                <input {...register('shiftAllowance', { valueAsNumber: true })} type="number" step="0.01" className="input" placeholder="0.00" />
              </FormField>
              <FormField label="Transportation Fee">
                <input {...register('transportationFee', { valueAsNumber: true })} type="number" step="0.01" className="input" placeholder="0.00" />
              </FormField>
              <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4 sm:col-span-2">
                <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">OT Rate / Hour</p>
                <p className="mt-2 text-lg font-semibold text-slate-900">{formatCurrency(autoOtRate)}</p>
                <p className="mt-1 text-xs text-slate-500">Auto calculated unless you type a custom amount below.</p>
              </div>
              <FormField label="OT Rate Override">
                <input {...register('otRatePerHour', { valueAsNumber: true })} type="number" step="0.01" className="input" placeholder="0.00" />
              </FormField>
              <FormField label="Std. Work Hours">
                <input {...register('standardWorkHours', { valueAsNumber: true })} type="number" step="1" className="input" placeholder="8" />
              </FormField>
              <FormField label="Status">
                <select {...register('status')} className="input">
                  <option value="Active">Active</option>
                  <option value="Inactive">Inactive</option>
                </select>
              </FormField>
            </div>
          </div>

          {editingProfile && (
            <button className="text-sm text-slate-500 hover:text-slate-800 underline" onClick={resetForm}>
              Cancel edit
            </button>
          )}
        </div>
      </div>
    </Modal>
  );
}

function PlusIcon() { return <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg>; }
function EditIcon() { return <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>; }
function TrashIcon() { return <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/></svg>; }

function MiniInfo({
  label,
  value,
  accent,
}: {
  label: string;
  value: string;
  accent?: string;
}) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-slate-50/80 p-3">
      <p className="text-[10px] uppercase tracking-[0.22em] text-slate-400">{label}</p>
      <p className={`mt-1 text-sm font-semibold ${accent ?? 'text-slate-800'}`}>{value}</p>
    </div>
  );
}
