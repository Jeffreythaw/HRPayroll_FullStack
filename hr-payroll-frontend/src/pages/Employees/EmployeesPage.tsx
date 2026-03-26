import { useEffect, useState, useCallback } from 'react';
import { useForm } from 'react-hook-form';
import toast from 'react-hot-toast';
import { employeeApi, departmentApi } from '../../api';
import {
  Modal, ConfirmModal, PageHeader, EmptyState, Spinner, FormField
} from '../../components/ui';
import { formatCurrency, formatDate, statusBadgeClass } from '../../utils';
import type { Employee, CreateEmployeeRequest, UpdateEmployeeRequest, Department } from '../../types';

type EmployeeFormValues = {
  name: string;
  finNo?: string;
  phone?: string;
  departmentId: number;
  joinDate: string;
  basicSalary: number;
  shiftAllowance: number;
  sundayPhOtDays: number;
  publicHolidayOtHours: number;
  transportationFee: number;
  deductionNoWork4Days: number;
  advanceSalary: number;
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
      <div className="card p-4 mb-4 flex flex-wrap items-center gap-3">
        <input
          value={search}
          onChange={e => setSearch(e.target.value)}
          className="input w-56"
          placeholder="Search name, code, email…"
        />
        <select value={filterStatus} onChange={e => setFilterStatus(e.target.value)} className="input w-36">
          <option value="">All Status</option>
          <option value="Active">Active</option>
          <option value="Inactive">Inactive</option>
        </select>
        <select value={filterDept} onChange={e => setFilterDept(e.target.value)} className="input w-48">
          <option value="">All Departments</option>
          {departments.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
        </select>
        <span className="text-sm text-slate-500 ml-auto">{filtered.length} result{filtered.length !== 1 ? 's' : ''}</span>
      </div>

      {/* Table */}
      <div className="card overflow-x-auto">
        {loading ? (
          <div className="flex justify-center py-16"><Spinner size="lg" /></div>
        ) : filtered.length === 0 ? (
          <EmptyState message="No employees found" />
        ) : (
          <table className="w-full">
            <thead>
              <tr>
                <th className="table-th">Employee</th>
                <th className="table-th">Department</th>
                <th className="table-th">Position</th>
                <th className="table-th">Basic Salary</th>
                <th className="table-th">OT Rate/Hr</th>
                <th className="table-th">Join Date</th>
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
      joinDate: new Date().toISOString().split('T')[0],
      basicSalary: 0,
      shiftAllowance: 0,
      sundayPhOtDays: 0,
      publicHolidayOtHours: 0,
      transportationFee: 0,
      deductionNoWork4Days: 0,
      advanceSalary: 0,
      departmentId: 0,
      status: 'Active',
    }
  });
  const basicSalary = watch('basicSalary') || 0;
  const autoOtRate = Math.max(0, (basicSalary / 24 / 11) * 1.5);

  useEffect(() => {
    if (employee) {
      reset({
        name: employee.fullName.trim(),
        finNo: employee.finNo || '',
        phone: employee.phone,
        departmentId: employee.departmentId,
        basicSalary: employee.basicSalary,
        shiftAllowance: employee.shiftAllowance,
        sundayPhOtDays: employee.sundayPhOtDays,
        publicHolidayOtHours: employee.publicHolidayOtHours,
        transportationFee: employee.transportationFee,
        deductionNoWork4Days: employee.deductionNoWork4Days,
        advanceSalary: employee.advanceSalary,
        joinDate: employee.joinDate,
        status: employee.status,
      });
    } else {
      reset({
        name: '',
        finNo: '',
        phone: '',
        departmentId: 0,
        basicSalary: 0,
        shiftAllowance: 0,
        sundayPhOtDays: 0,
        publicHolidayOtHours: 0,
        transportationFee: 0,
        deductionNoWork4Days: 0,
        advanceSalary: 0,
        status: 'Active',
        joinDate: new Date().toISOString().split('T')[0],
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
        departmentId: data.departmentId,
        position: isEdit && employee ? employee.position : 'Employee',
        basicSalary: data.basicSalary,
        shiftAllowance: data.shiftAllowance,
        otRatePerHour: autoOtRate,
        sundayPhOtDays: data.sundayPhOtDays,
        publicHolidayOtHours: data.publicHolidayOtHours,
        transportationFee: data.transportationFee,
        deductionNoWork4Days: data.deductionNoWork4Days,
        advanceSalary: data.advanceSalary,
        standardWorkHours: 8,
        joinDate: data.joinDate,
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
              <FormField label="Department" error={errors.departmentId?.message} required>
                <select {...register('departmentId', { required: 'Required', valueAsNumber: true })} className="input">
                  <option value="">Select department</option>
                  {departments.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
                </select>
              </FormField>
              <FormField label="Join Date" error={errors.joinDate?.message} required>
                <input {...register('joinDate', { required: 'Required' })} type="date" className="input" />
              </FormField>
            </div>
          </div>

          <div>
            <h3 className="text-sm font-semibold text-slate-700 mb-3">Salary & Allowances</h3>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <FormField label="Monthly Salary BASIC S$" error={errors.basicSalary?.message} required>
                <input {...register('basicSalary', { required: 'Required', valueAsNumber: true, min: { value: 0, message: 'Must be positive' } })} type="number" step="0.01" className="input" placeholder="3000.00" />
              </FormField>
              <FormField label="Shift Allowance S$">
                <input {...register('shiftAllowance', { valueAsNumber: true })} type="number" step="0.01" className="input" placeholder="0.00" />
              </FormField>
              <FormField label="Transportation Fee">
                <input {...register('transportationFee', { valueAsNumber: true })} type="number" step="0.01" className="input" placeholder="0.00" />
              </FormField>
              <FormField label="Advance Salary">
                <input {...register('advanceSalary', { valueAsNumber: true })} type="number" step="0.01" className="input" placeholder="0.00" />
              </FormField>
              <FormField label="Deduction (No Work / 4 days)">
                <input {...register('deductionNoWork4Days', { valueAsNumber: true })} type="number" step="0.01" className="input" placeholder="0.00" />
              </FormField>
              <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">OT Rate / Hour</p>
                <p className="mt-2 text-lg font-semibold text-slate-900">{formatCurrency(autoOtRate)}</p>
                <p className="mt-1 text-xs text-slate-500">Auto: basic / 24 / 11 x 1.5</p>
              </div>
            </div>
          </div>
        </div>

        <div className="space-y-4">
          <div>
            <h3 className="text-sm font-semibold text-slate-700 mb-3">OT Details</h3>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <FormField label="OT @ Sunday/P.H (days)">
                <input {...register('sundayPhOtDays', { valueAsNumber: true })} type="number" step="0.01" className="input" placeholder="0" />
              </FormField>
              <FormField label="OT @ Public Holiday (hrs)">
                <input {...register('publicHolidayOtHours', { valueAsNumber: true })} type="number" step="0.01" className="input" placeholder="0" />
              </FormField>
            </div>
          </div>

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

function PlusIcon() { return <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg>; }
function EditIcon() { return <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>; }
function TrashIcon() { return <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/></svg>; }
