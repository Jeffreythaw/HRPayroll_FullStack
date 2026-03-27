import { useEffect, useState, useCallback } from 'react';
import { useForm } from 'react-hook-form';
import toast from 'react-hot-toast';
import { payrollApi, employeeApi, employeeProfileApi } from '../../api';
import {
  Modal, ConfirmModal, PageHeader, EmptyState, Spinner,
  MonthYearPicker, StatCard, FormField
} from '../../components/ui';
import { formatCurrency, statusBadgeClass, payrollStatuses, currentMonthYear } from '../../utils';
import type { PayrollRecord, Employee, EmployeePayrollProfile, ProcessPayrollRequest, UpdatePayrollStatusRequest } from '../../types';

export default function PayrollPage() {
  const { month, year } = currentMonthYear();
  const [selMonth, setSelMonth] = useState(month);
  const [selYear, setSelYear] = useState(year);
  const [records, setRecords] = useState<PayrollRecord[]>([]);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [loading, setLoading] = useState(true);

  const [showProcess, setShowProcess] = useState(false);
  const [showStatusModal, setShowStatusModal] = useState(false);
  const [statusTarget, setStatusTarget] = useState<PayrollRecord | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<PayrollRecord | null>(null);
  const [deleting, setDeleting] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [recs, emps] = await Promise.all([
        payrollApi.getByMonth(selMonth, selYear),
        employeeApi.getAll({ status: 'Active' }),
      ]);
      setRecords(recs);
      setEmployees(emps);
    } finally { setLoading(false); }
  }, [selMonth, selYear]);

  useEffect(() => { load(); }, [load]);

  const handleDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    try {
      await payrollApi.delete(deleteTarget.id);
      toast.success('Payroll record deleted');
      setDeleteTarget(null);
      load();
    } catch { toast.error('Failed to delete record'); }
    finally { setDeleting(false); }
  };

  const totalNet = records.reduce((s, r) => s + r.netSalary, 0);
  const totalOT  = records.reduce((s, r) => s + r.otAmount, 0);
  const totalDed = records.reduce((s, r) => s + r.deductions, 0);
  const paidCount = records.filter(r => r.status === 'Paid').length;

  return (
    <div>
      <PageHeader
        title="Payroll"
        subtitle="Process and manage monthly payroll"
        actions={
          <button className="btn-primary" onClick={() => setShowProcess(true)}>
            <ProcessIcon /> Process Payroll
          </button>
        }
      />

      {/* Month picker */}
      <div className="card p-4 mb-4 flex flex-col gap-3 sm:flex-row sm:items-center">
        <MonthYearPicker month={selMonth} year={selYear} onChange={(m, y) => { setSelMonth(m); setSelYear(y); }} />
        <span className="text-sm text-slate-500 sm:ml-auto">{records.length} records</span>
      </div>

      {/* Summary cards */}
      {records.length > 0 && (
        <div className="grid grid-cols-2 xl:grid-cols-4 gap-4 mb-4">
          <StatCard label="Total Net Salary" value={formatCurrency(totalNet)} icon={<DollarIcon />} color="bg-navy-800" />
          <StatCard label="Total OT Amount"  value={formatCurrency(totalOT)}  icon={<OTIcon />}     color="bg-indigo-600" />
          <StatCard label="Total Deductions" value={formatCurrency(totalDed)} icon={<MinusIcon />}   color="bg-red-500" />
          <StatCard label="Paid"             value={`${paidCount}/${records.length}`} icon={<CheckIcon />} color="bg-emerald-600" sub="employees paid" />
        </div>
      )}

      {/* Records */}
      <div className="card overflow-hidden">
        {loading ? (
          <div className="flex justify-center py-16"><Spinner size="lg" /></div>
        ) : records.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-16 text-slate-400 px-4 text-center">
            <ProcessIcon2 />
            <p className="text-sm font-medium mt-3">No payroll records for this month</p>
            <p className="text-xs mt-1">Click "Process Payroll" to calculate salaries</p>
          </div>
        ) : (
          <>
            <div className="lg:hidden divide-y divide-slate-100">
              {records.map(r => (
                <div key={r.id} className="p-4 space-y-4">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="font-semibold text-slate-900 leading-tight">{r.employeeName}</p>
                      <p className="text-xs font-mono text-slate-400">{r.employeeCode}</p>
                      <p className="text-xs text-slate-500 mt-1">{r.departmentName}</p>
                    </div>
                    <button
                      onClick={() => { setStatusTarget(r); setShowStatusModal(true); }}
                      className={`${statusBadgeClass(r.status)} cursor-pointer hover:opacity-80`}
                    >
                      {r.status}
                    </button>
                  </div>

                  <div className="grid grid-cols-2 gap-3">
                    <MetricCard label="Present / Absent / Leave" value={`${r.presentDays} / ${r.absentDays} / ${r.leaveDays}`} />
                    <MetricCard label="Work Days" value={`${r.workingDays}`} />
                    <MetricCard label="Work Hours" value={`${r.totalWorkHours.toFixed(1)}h`} />
                    <MetricCard label="OT Hours" value={`${r.totalOTHours.toFixed(1)}h`} accent="text-indigo-600" />
                    <MetricCard label="Basic" value={formatCurrency(r.basicSalary)} />
                    <MetricCard label="OT Amt" value={formatCurrency(r.otAmount)} accent="text-indigo-600" />
                    <MetricCard label="Deduct." value={`-${formatCurrency(r.deductions)}`} accent="text-red-500" />
                    <MetricCard label="Net Salary" value={formatCurrency(r.netSalary)} strong />
                  </div>

                  <div className="flex items-center justify-end gap-2 pt-1">
                    <button
                      onClick={() => setDeleteTarget(r)}
                      className="btn-danger px-3 py-2 text-xs"
                    >
                      Delete
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
                    <th className="table-th">Days</th>
                    <th className="table-th">Work Hrs</th>
                    <th className="table-th">OT Hrs</th>
                    <th className="table-th">Basic</th>
                    <th className="table-th">OT Amt</th>
                    <th className="table-th">Deduct.</th>
                    <th className="table-th">Net Salary</th>
                    <th className="table-th">Status</th>
                    <th className="table-th w-20">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {records.map(r => (
                    <tr key={r.id} className="hover:bg-slate-50 transition-colors">
                      <td className="table-td">
                        <p className="font-semibold text-slate-900">{r.employeeName}</p>
                        <p className="text-xs font-mono text-slate-400">{r.employeeCode}</p>
                      </td>
                      <td className="table-td text-slate-500 text-xs">{r.departmentName}</td>
                      <td className="table-td">
                        <div className="text-xs">
                          <span className="text-emerald-600 font-semibold">{r.presentDays}P</span>
                          {' / '}
                          <span className="text-red-500">{r.absentDays}A</span>
                          {' / '}
                          <span className="text-amber-500">{r.leaveDays}L</span>
                        </div>
                        <p className="text-xs text-slate-400">{r.workingDays} work days</p>
                      </td>
                      <td className="table-td font-mono text-sm">{r.totalWorkHours.toFixed(1)}h</td>
                      <td className="table-td font-mono text-sm text-indigo-600">{r.totalOTHours.toFixed(1)}h</td>
                      <td className="table-td font-mono text-sm">{formatCurrency(r.basicSalary)}</td>
                      <td className="table-td font-mono text-sm text-indigo-600">{formatCurrency(r.otAmount)}</td>
                      <td className="table-td font-mono text-sm text-red-500">-{formatCurrency(r.deductions)}</td>
                      <td className="table-td">
                        <span className="font-bold font-mono text-slate-900">{formatCurrency(r.netSalary)}</span>
                      </td>
                      <td className="table-td">
                        <button
                          onClick={() => { setStatusTarget(r); setShowStatusModal(true); }}
                          className={`${statusBadgeClass(r.status)} cursor-pointer hover:opacity-80`}
                        >
                          {r.status}
                        </button>
                      </td>
                      <td className="table-td">
                        <button
                          onClick={() => setDeleteTarget(r)}
                          className="p-1.5 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded transition-colors"
                          title="Delete"
                        >
                          <TrashIcon />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </>
        )}
      </div>

      <ProcessPayrollModal
        open={showProcess}
        onClose={() => setShowProcess(false)}
        employees={employees}
        month={selMonth}
        year={selYear}
        onProcessed={() => { setShowProcess(false); load(); }}
      />

      <UpdateStatusModal
        open={showStatusModal}
        onClose={() => { setShowStatusModal(false); setStatusTarget(null); }}
        record={statusTarget}
        onSaved={() => { setShowStatusModal(false); setStatusTarget(null); load(); }}
      />

      <ConfirmModal
        open={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        onConfirm={handleDelete}
        title="Delete Payroll Record"
        message={`Delete payroll record for ${deleteTarget?.employeeName}?`}
        confirmLabel="Delete"
        loading={deleting}
      />
    </div>
  );
}

// ─── Process Payroll Modal ────────────────────────────────────
function ProcessPayrollModal({ open, onClose, employees, month, year, onProcessed }: {
  open: boolean; onClose: () => void; employees: Employee[];
  month: number; year: number; onProcessed: () => void;
}) {
  const [processing, setProcessing] = useState(false);
  const [selectedIds, setSelectedIds] = useState<number[]>([]);
  const [profiles, setProfiles] = useState<EmployeePayrollProfile[]>([]);
  const [adjustments, setAdjustments] = useState<Record<number, { advanceSalary: number; deductionNoWork4Days: number }>>({});
  const allSelected = selectedIds.length === 0;

  const toggle = (id: number) => {
    setSelectedIds(prev => prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id]);
  };

  useEffect(() => {
    if (!open) return;
    setSelectedIds([]);
    let mounted = true;
    employeeProfileApi.getAll({ status: 'Active' })
      .then(data => { if (mounted) setProfiles(data); })
      .catch(() => { if (mounted) setProfiles([]); });
    return () => { mounted = false; };
  }, [open]);

  const targetProfiles = profiles.filter(profile => {
    if (allSelected) return true;
    return selectedIds.includes(profile.employeeId);
  });

  useEffect(() => {
    if (!open) return;
    setAdjustments(prev => {
      const next: Record<number, { advanceSalary: number; deductionNoWork4Days: number }> = {};
      for (const profile of targetProfiles) {
        next[profile.id] = prev[profile.id] ?? { advanceSalary: 0, deductionNoWork4Days: 0 };
      }
      return next;
    });
  }, [open, profiles, selectedIds, allSelected]);

  const handleProcess = async () => {
    setProcessing(true);
    try {
      const req: ProcessPayrollRequest = {
        month, year,
        employeeIds: selectedIds.length > 0 ? selectedIds : undefined,
        adjustments: Object.entries(adjustments).map(([profileId, value]) => ({
          employeePayrollProfileId: Number(profileId),
          advanceSalary: Number(value.advanceSalary) || 0,
          deductionNoWork4Days: Number(value.deductionNoWork4Days) || 0,
        })),
      };
      const results = await payrollApi.process(req);
      toast.success(`Processed payroll for ${results.length} employee(s)`);
      onProcessed();
    } catch (err: any) {
      toast.error(err?.response?.data?.message || 'Failed to process payroll');
    } finally { setProcessing(false); }
  };

  const { MONTHS } = { MONTHS: ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'] };

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Process Monthly Payroll"
      size="md"
      footer={
        <>
          <button className="btn-secondary" onClick={onClose} disabled={processing}>Cancel</button>
          <button className="btn-success" onClick={handleProcess} disabled={processing}>
            {processing ? <><Spinner size="sm" /> Processing…</> : <><ProcessIcon /> Process {MONTHS[month-1]} {year}</>}
          </button>
        </>
      }
    >
      <div className="space-y-4">
        <div className="bg-amber-50 border border-amber-200 rounded-lg p-3 text-sm text-amber-700">
          <strong>Note:</strong> This will calculate salaries based on attendance records.
          Existing draft records will be recalculated.
        </div>
        <div>
          <p className="text-sm font-semibold text-slate-700 mb-2">
            Select Employees{' '}
            <span className="text-slate-400 font-normal">(leave all unselected to process all active)</span>
          </p>
          <div className="max-h-56 overflow-y-auto space-y-1 border border-slate-200 rounded-lg p-2">
            {employees.map(e => (
              <label key={e.id} className="flex items-center gap-2 p-2 hover:bg-slate-50 rounded cursor-pointer">
                <input
                  type="checkbox"
                  checked={selectedIds.includes(e.id)}
                  onChange={() => toggle(e.id)}
                  className="rounded"
                />
                <span className="text-sm">{e.fullName}</span>
                <span className="text-xs text-slate-400 font-mono ml-auto">{e.employeeCode}</span>
              </label>
            ))}
          </div>
          {allSelected && (
            <p className="text-xs text-slate-500 mt-1">All {employees.length} active employees will be processed</p>
          )}
        </div>

        <div className="mt-4 rounded-2xl border border-slate-200 bg-slate-50 p-4">
          <div className="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <p className="text-[10px] font-bold uppercase tracking-[0.24em] text-slate-400">Monthly adjustments</p>
              <p className="text-sm text-slate-600">
                Enter only for the current payroll month. Leave blank or zero if not needed.
              </p>
            </div>
            <p className="text-xs text-slate-500">{targetProfiles.length} profile(s) selected</p>
          </div>

          <div className="mt-3 overflow-x-auto">
            <table className="w-full min-w-[720px]">
              <thead>
                <tr>
                  <th className="table-th">Employee</th>
                  <th className="table-th">Profile</th>
                  <th className="table-th">Advance Salary</th>
                  <th className="table-th">No Work Deduction</th>
                </tr>
              </thead>
              <tbody>
                {targetProfiles.map(profile => (
                  <tr key={profile.id} className="border-t border-slate-200">
                    <td className="table-td">
                      <p className="font-semibold text-slate-900">{profile.employeeName}</p>
                      <p className="text-xs text-slate-400 font-mono">{profile.employeeCode}</p>
                    </td>
                    <td className="table-td">
                      <p className="text-sm text-slate-700">{profile.profileName}</p>
                      <p className="text-xs text-slate-400">{profile.salaryMode}</p>
                    </td>
                    <td className="table-td">
                      <input
                        type="number"
                        step="0.01"
                        className="input"
                        value={adjustments[profile.id]?.advanceSalary ?? 0}
                        onChange={e => setAdjustments(prev => ({
                          ...prev,
                          [profile.id]: {
                            advanceSalary: Number(e.target.value) || 0,
                            deductionNoWork4Days: prev[profile.id]?.deductionNoWork4Days ?? 0,
                          }
                        }))}
                        placeholder="0.00"
                      />
                    </td>
                    <td className="table-td">
                      <input
                        type="number"
                        step="0.01"
                        className="input"
                        value={adjustments[profile.id]?.deductionNoWork4Days ?? 0}
                        onChange={e => setAdjustments(prev => ({
                          ...prev,
                          [profile.id]: {
                            advanceSalary: prev[profile.id]?.advanceSalary ?? 0,
                            deductionNoWork4Days: Number(e.target.value) || 0,
                          }
                        }))}
                        placeholder="0.00"
                      />
                    </td>
                  </tr>
                ))}
                {targetProfiles.length === 0 && (
                  <tr>
                    <td className="table-td text-slate-400" colSpan={4}>Select employees to load payroll profiles.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </Modal>
  );
}

// ─── Update Status Modal ──────────────────────────────────────
function UpdateStatusModal({ open, onClose, record, onSaved }: {
  open: boolean; onClose: () => void; record: PayrollRecord | null; onSaved: () => void;
}) {
  const [saving, setSaving] = useState(false);
  const { register, handleSubmit, reset } = useForm<UpdatePayrollStatusRequest>({
    defaultValues: { status: 'Draft' }
  });

  useEffect(() => {
    if (record) reset({ status: record.status, notes: record.notes });
  }, [record, reset]);

  const onSubmit = async (data: UpdatePayrollStatusRequest) => {
    if (!record) return;
    setSaving(true);
    try {
      await payrollApi.updateStatus(record.id, data);
      toast.success('Status updated');
      onSaved();
    } catch { toast.error('Failed to update status'); }
    finally { setSaving(false); }
  };

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Update Payroll Status"
      size="sm"
      footer={
        <>
          <button className="btn-secondary" onClick={onClose} disabled={saving}>Cancel</button>
          <button className="btn-primary" onClick={handleSubmit(onSubmit)} disabled={saving}>
            {saving ? <><Spinner size="sm" /> Saving…</> : 'Update Status'}
          </button>
        </>
      }
    >
      <div className="space-y-4">
        <p className="text-sm text-slate-600">
          Updating status for <strong>{record?.employeeName}</strong>
        </p>
        <FormField label="Status">
          <select {...register('status')} className="input">
            {payrollStatuses().map(s => <option key={s} value={s}>{s}</option>)}
          </select>
        </FormField>
        <FormField label="Notes">
          <textarea {...register('notes')} className="input h-20 resize-none" placeholder="Optional notes…" />
        </FormField>
      </div>
    </Modal>
  );
}

function DollarIcon()   { return <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><line x1="12" y1="1" x2="12" y2="23"/><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/></svg>; }
function OTIcon()       { return <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>; }
function MinusIcon()    { return <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5"><line x1="5" y1="12" x2="19" y2="12"/></svg>; }
function CheckIcon()    { return <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5"><polyline points="20 6 9 17 4 12"/></svg>; }
function ProcessIcon()  { return <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><polyline points="17 1 21 5 17 9"/><path d="M3 11V9a4 4 0 0 1 4-4h14"/><polyline points="7 23 3 19 7 15"/><path d="M21 13v2a4 4 0 0 1-4 4H3"/></svg>; }
function ProcessIcon2() { return <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5"><polyline points="17 1 21 5 17 9"/><path d="M3 11V9a4 4 0 0 1 4-4h14"/><polyline points="7 23 3 19 7 15"/><path d="M21 13v2a4 4 0 0 1-4 4H3"/></svg>; }
function TrashIcon()    { return <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/></svg>; }

function MetricCard({
  label,
  value,
  accent,
  strong,
}: {
  label: string;
  value: string;
  accent?: string;
  strong?: boolean;
}) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-slate-50/80 p-3">
      <p className="text-[10px] uppercase tracking-[0.22em] text-slate-400">{label}</p>
      <p className={`mt-1 text-sm ${strong ? 'font-bold text-slate-900' : `font-semibold ${accent ?? 'text-slate-800'}`}`}>{value}</p>
    </div>
  );
}
