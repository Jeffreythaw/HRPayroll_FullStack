import { useEffect, useState, useCallback, useRef } from 'react';
import { useForm } from 'react-hook-form';
import toast from 'react-hot-toast';
import { attendanceApi, attendanceLookupApi, employeeApi, publicHolidayApi } from '../../api';
import {
  Modal, ConfirmModal, PageHeader, EmptyState, Spinner,
  MonthYearPicker, FormField
} from '../../components/ui';
import { formatTime, formatDate, statusBadgeClass, attendanceStatuses, currentMonthYear } from '../../utils';
import type {
  Attendance, AttendanceLookup, CreateAttendanceRequest, UpdateAttendanceRequest, Employee, PublicHoliday
} from '../../types';

export default function AttendancePage() {
  const { month, year } = currentMonthYear();
  const [selMonth, setSelMonth] = useState(month);
  const [selYear, setSelYear] = useState(year);
  const [records, setRecords] = useState<Attendance[]>([]);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [lookups, setLookups] = useState<AttendanceLookup[]>([]);
  const [publicHolidays, setPublicHolidays] = useState<PublicHoliday[]>([]);
  const [loading, setLoading] = useState(true);
  const [holidayLoading, setHolidayLoading] = useState(false);
  const [holidaySyncing, setHolidaySyncing] = useState(false);
  const [filterEmp, setFilterEmp] = useState('');
  const [filterStatus, setFilterStatus] = useState('');

  const [showForm, setShowForm] = useState(false);
  const [showLookupManager, setShowLookupManager] = useState(false);
  const [editing, setEditing] = useState<Attendance | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<Attendance | null>(null);
  const [deleting, setDeleting] = useState(false);
  const initialPeriodRef = useRef({ month, year });
  const autoFallbackUsedRef = useRef(false);

  const load = useCallback(async () => {
    setLoading(true);
    setHolidayLoading(true);
    try {
      const [recs, emps, lookupItems] = await Promise.all([
        attendanceApi.getByMonth(selMonth, selYear),
        employeeApi.getAll({ status: 'Active' }),
        attendanceLookupApi.getAll(),
      ]);
      const holidayItems = await publicHolidayApi.getByYear(selYear).catch(() => []);

      if (
        !autoFallbackUsedRef.current &&
        recs.length === 0 &&
        selMonth === initialPeriodRef.current.month &&
        selYear === initialPeriodRef.current.year
      ) {
        const fallback = await findLatestAttendancePeriod(initialPeriodRef.current.year, initialPeriodRef.current.month);
        if (fallback) {
          autoFallbackUsedRef.current = true;
          setSelMonth(fallback.month);
          setSelYear(fallback.year);
          setRecords(fallback.records);
          setEmployees(emps);
          setLookups(lookupItems);
          return;
        }
      }

      setRecords(recs);
      setEmployees(emps);
      setLookups(lookupItems);
      setPublicHolidays(holidayItems);
    } finally {
      setLoading(false);
      setHolidayLoading(false);
    }
  }, [selMonth, selYear]);

  useEffect(() => { load(); }, [load]);

  const filtered = records.filter(r => {
    const matchEmp = !filterEmp || String(r.employeeId) === filterEmp;
    const matchStatus = !filterStatus || r.status === filterStatus;
    return matchEmp && matchStatus;
  });

  const openCreate = () => { setEditing(null); setShowForm(true); };
  const openEdit = (r: Attendance) => { setEditing(r); setShowForm(true); };
  const closeForm = () => { setShowForm(false); setEditing(null); };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    try {
      await attendanceApi.delete(deleteTarget.id);
      toast.success('Record deleted');
      setDeleteTarget(null);
      load();
    } catch {
      toast.error('Failed to delete record');
    } finally {
      setDeleting(false);
    }
  };

  const totalPresent = records.filter(r => r.status === 'Present').length;
  const totalAbsent = records.filter(r => r.status === 'Absent').length;
  const totalWorkHours = records.reduce((s, r) => s + r.workHours, 0);
  const totalOTHours = records.reduce((s, r) => s + r.otHours, 0);

  const siteProjectOptions = lookups
    .filter(x => x.category === 'SiteProject' && x.isActive)
    .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name));

  const transportOptions = lookups
    .filter(x => x.category === 'Transport' && x.isActive)
    .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name));

  const holidayMonthPrefix = `${selYear}-${String(selMonth).padStart(2, '0')}`;
  const holidayMonthItems = publicHolidays
    .filter(h => h.date.startsWith(holidayMonthPrefix))
    .sort((a, b) => a.date.localeCompare(b.date));

  const syncHolidays = async () => {
    setHolidaySyncing(true);
    try {
      const synced = await publicHolidayApi.sync(selYear);
      setPublicHolidays(synced);
      toast.success(`Singapore public holidays synced for ${selYear}`);
    } catch (err: any) {
      toast.error(err?.response?.data?.message || 'Failed to sync public holidays');
    } finally {
      setHolidaySyncing(false);
    }
  };

  return (
    <div>
      <PageHeader
        title="Attendance"
        subtitle="Track daily attendance records"
        actions={
          <>
            <button className="btn-secondary" onClick={() => setShowLookupManager(true)}>
              <DropdownIcon /> Manage Dropdowns
            </button>
            <button className="btn-primary" onClick={openCreate}>
              <PlusIcon /> Add Record
            </button>
          </>
        }
      />

      <div className="card p-4 mb-4 grid grid-cols-1 gap-3 md:grid-cols-2 xl:flex xl:flex-wrap xl:items-center">
        <div className="w-full xl:w-auto">
          <MonthYearPicker month={selMonth} year={selYear} onChange={(m, y) => { setSelMonth(m); setSelYear(y); }} />
        </div>
        <select value={filterEmp} onChange={e => setFilterEmp(e.target.value)} className="input w-full md:w-auto md:min-w-48">
          <option value="">All Employees</option>
          {employees.map(e => <option key={e.id} value={e.id}>{e.fullName}</option>)}
        </select>
        <select value={filterStatus} onChange={e => setFilterStatus(e.target.value)} className="input w-full md:w-auto md:min-w-36">
          <option value="">All Status</option>
          {attendanceStatuses().map(s => <option key={s} value={s}>{s}</option>)}
        </select>
        <div className="flex flex-wrap items-center gap-4 text-xs text-slate-500 xl:ml-auto">
          <span><span className="font-semibold text-emerald-600">{totalPresent}</span> Present</span>
          <span><span className="font-semibold text-red-500">{totalAbsent}</span> Absent</span>
          <span><span className="font-semibold text-slate-700">{totalWorkHours.toFixed(1)}h</span> Working</span>
          <span><span className="font-semibold text-indigo-600">{totalOTHours.toFixed(1)}h</span> OT</span>
        </div>
      </div>

      <div className="card mb-4 p-4">
        <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <p className="text-[10px] font-bold uppercase tracking-[0.28em] text-slate-400">Singapore Public Holidays</p>
            <h2 className="mt-1 text-sm font-semibold text-slate-900">
              Official {selYear} holidays are synced from MOM / data.gov.sg
            </h2>
            <p className="mt-1 text-xs text-slate-500">
              Payroll uses this list automatically, so holidays do not need to be keyed in every year.
            </p>
          </div>
          <button className="btn-secondary" onClick={syncHolidays} disabled={holidaySyncing}>
            {holidaySyncing ? <><Spinner size="sm" /> Syncing…</> : 'Sync Holidays'}
          </button>
        </div>
        <div className="mt-4 flex flex-wrap gap-2">
          {holidayLoading ? (
            <span className="text-sm text-slate-400">Loading holidays…</span>
          ) : holidayMonthItems.length > 0 ? (
            holidayMonthItems.map(h => (
              <span key={h.id} className="inline-flex items-center gap-2 rounded-full border border-slate-200 bg-slate-50 px-3 py-1 text-xs text-slate-700">
                <span className="font-semibold">{formatDate(h.date)}</span>
                <span className="text-slate-400">·</span>
                <span>{h.name}</span>
              </span>
            ))
          ) : (
            <span className="text-sm text-slate-400">No public holidays in this month.</span>
          )}
        </div>
      </div>

      <div className="card lg:hidden">
        {loading ? (
          <div className="flex justify-center py-16"><Spinner size="lg" /></div>
        ) : filtered.length === 0 ? (
          <EmptyState message="No attendance records for this period" />
        ) : (
          <div className="p-4 space-y-3">
            {filtered.map(r => (
              <div key={r.id} className="rounded-2xl border border-slate-200/80 bg-white p-4 shadow-sm">
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <p className="font-semibold text-slate-900 leading-tight">{r.employeeName}</p>
                    <p className="text-xs text-slate-400 font-mono">{r.employeeCode}</p>
                  </div>
                  <span className={statusBadgeClass(r.status)}>{r.status}</span>
                </div>
                <div className="mt-3 grid grid-cols-2 gap-3 text-sm">
                  <InfoPair label="Date" value={r.date} />
                  <InfoPair label="Start" value={formatTime(r.start)} mono />
                  <InfoPair label="End" value={formatTime(r.end)} mono />
                  <InfoPair label="Work" value={r.workHours > 0 ? `${r.workHours.toFixed(2)}h` : '—'} mono />
                  <InfoPair label="OT" value={r.otHours > 0 ? `${r.otHours.toFixed(2)}h` : '—'} mono />
                  <InfoPair label="Transport" value={r.transport || '—'} />
                </div>
                {r.isOvernight && (
                  <div className="mt-3">
                    <span className="inline-flex rounded-full border border-indigo-200 bg-indigo-50 px-2.5 py-1 text-[10px] font-semibold uppercase tracking-[0.22em] text-indigo-700">
                      Night Shift
                    </span>
                  </div>
                )}
                <div className="mt-3 text-sm">
                  <p className="text-[10px] uppercase tracking-[0.24em] text-slate-400 mb-1">Site / Project</p>
                  <p className="text-slate-700">{r.siteProject || '—'}</p>
                </div>
                {r.remarks && (
                  <div className="mt-2 text-sm">
                    <p className="text-[10px] uppercase tracking-[0.24em] text-slate-400 mb-1">Remarks</p>
                    <p className="text-slate-500">{r.remarks}</p>
                  </div>
                )}
                <div className="mt-4 flex items-center justify-end gap-2">
                  <button onClick={() => openEdit(r)} className="btn-secondary px-3 py-2 text-xs">Edit</button>
                  <button onClick={() => setDeleteTarget(r)} className="btn-danger px-3 py-2 text-xs">Delete</button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      <div className="card overflow-x-auto hidden lg:block">
        {loading ? (
          <div className="flex justify-center py-16"><Spinner size="lg" /></div>
        ) : filtered.length === 0 ? (
          <EmptyState message="No attendance records for this period" />
        ) : (
          <table className="w-full">
            <thead>
              <tr>
                <th className="table-th">Employee</th>
                <th className="table-th">Date</th>
                <th className="table-th">Start</th>
                <th className="table-th">End</th>
                <th className="table-th">Working Hours</th>
                <th className="table-th">OT Hours</th>
                <th className="table-th">Night Shift</th>
                <th className="table-th">Site / Project</th>
                <th className="table-th">Transport</th>
                <th className="table-th">Status</th>
                <th className="table-th">Remarks</th>
                <th className="table-th w-20">Actions</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map(r => (
                <tr key={r.id} className="hover:bg-slate-50 transition-colors">
                  <td className="table-td">
                    <p className="font-medium text-slate-900">{r.employeeName}</p>
                    <p className="text-xs text-slate-400 font-mono">{r.employeeCode}</p>
                  </td>
                  <td className="table-td text-slate-600">{r.date}</td>
                  <td className="table-td font-mono">{formatTime(r.start)}</td>
                  <td className="table-td font-mono">{formatTime(r.end)}</td>
                  <td className="table-td">
                    {r.workHours > 0 ? (
                      <span className="font-mono text-slate-700">{r.workHours.toFixed(2)}h</span>
                    ) : '—'}
                  </td>
                  <td className="table-td">
                    {r.otHours > 0 ? (
                      <span className="font-mono text-indigo-600 font-semibold">{r.otHours.toFixed(2)}h</span>
                    ) : <span className="text-slate-400">—</span>}
                  </td>
                  <td className="table-td">
                    {r.isOvernight ? <span className="badge-purple">Night Shift</span> : <span className="text-slate-400">—</span>}
                  </td>
                  <td className="table-td text-slate-600 text-sm">{r.siteProject || '—'}</td>
                  <td className="table-td text-slate-600 text-sm">{r.transport || '—'}</td>
                  <td className="table-td">
                    <span className={statusBadgeClass(r.status)}>{r.status}</span>
                  </td>
                  <td className="table-td text-slate-500 text-xs max-w-[140px] truncate">{r.remarks || '—'}</td>
                  <td className="table-td">
                    <div className="flex items-center gap-1">
                      <button onClick={() => openEdit(r)} className="p-1.5 text-slate-400 hover:text-navy-800 hover:bg-navy-50 rounded transition-colors" title="Edit"><EditIcon /></button>
                      <button onClick={() => setDeleteTarget(r)} className="p-1.5 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded transition-colors" title="Delete"><TrashIcon /></button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      <AttendanceFormModal
        open={showForm}
        onClose={closeForm}
        record={editing}
        employees={employees}
        siteProjects={siteProjectOptions}
        transports={transportOptions}
        defaultMonth={selMonth}
        defaultYear={selYear}
        onSaved={() => { closeForm(); load(); }}
      />

      <LookupManagerModal
        open={showLookupManager}
        onClose={() => setShowLookupManager(false)}
        lookups={lookups}
        onSaved={load}
      />

      <ConfirmModal
        open={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        onConfirm={handleDelete}
        title="Delete Attendance Record"
        message={`Delete attendance record for ${deleteTarget?.employeeName} on ${deleteTarget?.date}?`}
        confirmLabel="Delete"
        loading={deleting}
      />
    </div>
  );
}

async function findLatestAttendancePeriod(startYear: number, startMonth: number) {
  for (let offset = 1; offset <= 12; offset++) {
    const date = new Date(startYear, startMonth - 1 - offset, 1);
    const month = date.getMonth() + 1;
    const year = date.getFullYear();
    const records = await attendanceApi.getByMonth(month, year);
    if (records.length > 0) {
      return { month, year, records };
    }
  }
  return null;
}

// ─── Attendance Form Modal ────────────────────────────────────
interface AttendanceFormModalProps {
  open: boolean;
  onClose: () => void;
  record: Attendance | null;
  employees: Employee[];
  siteProjects: AttendanceLookup[];
  transports: AttendanceLookup[];
  defaultMonth: number;
  defaultYear: number;
  onSaved: () => void;
}

function AttendanceFormModal({
  open, onClose, record, employees, siteProjects, transports, defaultMonth, defaultYear, onSaved
}: AttendanceFormModalProps) {
  const isEdit = !!record;
  const [saving, setSaving] = useState(false);

  const { register, handleSubmit, reset, watch, formState: { errors } } = useForm<CreateAttendanceRequest>({
    defaultValues: { status: 'Present', isOvernight: false }
  });
  const status = watch('status');
  const start = watch('start');
  const end = watch('end');
  const isOvernight = watch('isOvernight');
  const employeeId = watch('employeeId');
  const selectedEmployee = employees.find(e => e.id === Number(employeeId));
  const standardHours = selectedEmployee?.standardWorkHours ?? 8;

  useEffect(() => {
    if (record) {
      reset({
        employeeId: record.employeeId,
        date: record.date,
        start: record.start,
        end: record.end,
        isOvernight: record.isOvernight ?? isOvernightPunch(record.start, record.end),
        siteProject: record.siteProject,
        transport: record.transport,
        status: record.status,
        remarks: record.remarks,
      });
    } else {
      const d = new Date(defaultYear, defaultMonth - 1, new Date().getDate());
      reset({
        date: d.toISOString().split('T')[0],
        status: 'Present',
        isOvernight: false,
        siteProject: '',
        transport: '',
      });
    }
  }, [record, reset, defaultMonth, defaultYear]);

  const onSubmit = async (data: CreateAttendanceRequest) => {
    setSaving(true);
    try {
      const payload: CreateAttendanceRequest = {
        ...data,
        start: normalizeTimeInput(data.start),
        end: normalizeTimeInput(data.end),
        isOvernight: data.status === 'Present' || data.status === 'HalfDay'
          ? !!data.isOvernight || isOvernightPunch(data.start, data.end)
          : false,
      };
      if (payload.status !== 'Present' && payload.status !== 'HalfDay') {
        payload.start = undefined;
        payload.end = undefined;
      }
      if (isEdit && record) {
        await attendanceApi.update(record.id, payload as UpdateAttendanceRequest);
        toast.success('Record updated');
      } else {
        await attendanceApi.create(payload);
        toast.success('Record added');
      }
      onSaved();
    } catch (err: any) {
      toast.error(err?.response?.data?.message || 'Failed to save record');
    } finally {
      setSaving(false);
    }
  };

  const needsTime = status === 'Present' || status === 'HalfDay';
  const preview = getAttendancePreview(start, end, standardHours, !!isOvernight || isOvernightPunch(start, end), needsTime);

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={isEdit ? 'Edit Attendance' : 'Add Attendance Record'}
      size="lg"
      footer={
        <>
          <button className="btn-secondary" onClick={onClose} disabled={saving}>Cancel</button>
          <button className="btn-primary" onClick={handleSubmit(onSubmit)} disabled={saving}>
            {saving ? <><Spinner size="sm" /> Saving…</> : (isEdit ? 'Save Changes' : 'Add Record')}
          </button>
        </>
      }
    >
      <div className="space-y-4">
        {!isEdit && (
          <FormField label="Employee" error={errors.employeeId?.message} required>
            <select {...register('employeeId', { required: 'Required', valueAsNumber: true })} className="input">
              <option value="">Select employee</option>
              {employees.map(e => <option key={e.id} value={e.id}>{e.fullName} ({e.employeeCode})</option>)}
            </select>
          </FormField>
        )}

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <FormField label="Date" error={errors.date?.message} required>
            <input {...register('date', { required: 'Required' })} type="date" className="input" />
          </FormField>
          <FormField label="Status" required>
            <select {...register('status')} className="input">
              {attendanceStatuses().map(s => <option key={s} value={s}>{s}</option>)}
            </select>
          </FormField>
          {needsTime && (
            <div className="md:col-span-2 grid grid-cols-1 md:grid-cols-2 gap-4 rounded-2xl border border-slate-200 bg-slate-50/80 p-4">
              <FormField label="Start" error={errors.start?.message} required>
                <input {...register('start', { required: 'Required' })} type="time" className="input" />
              </FormField>
              <FormField label="End" error={errors.end?.message} required>
                <input
                  {...register('end', {
                    required: 'Required',
                    validate: value => validateAttendanceRange(start, value, !!isOvernight, needsTime) || 'Check out must be later than check in unless this is a night shift.',
                  })}
                  type="time"
                  className="input"
                />
              </FormField>
              <label className="md:col-span-2 flex items-start gap-3 rounded-xl border border-slate-200 bg-white px-3 py-3 text-sm text-slate-700">
                <input
                  type="checkbox"
                  className="mt-1"
                  {...register('isOvernight')}
                />
                <span>
                  <span className="font-semibold text-slate-900">Night shift / next-day checkout</span>
                    <span className="block text-xs text-slate-500 mt-1">
                      Use this for punches like 9:00 PM → 9:00 AM. The backend will also auto-detect overnight punches when End is earlier than Start.
                    </span>
                </span>
              </label>
              <div className="md:col-span-2 rounded-xl border border-indigo-100 bg-indigo-50/70 p-3 text-sm text-slate-700">
                <p className="text-[10px] font-bold uppercase tracking-[0.24em] text-indigo-500">Live Preview</p>
                {preview.valid ? (
                  <div className="mt-2 grid grid-cols-2 gap-3 sm:grid-cols-4">
                    <PreviewStat label="Gross" value={`${preview.totalHours.toFixed(2)}h`} />
                    <PreviewStat label="Lunch" value={preview.lunchDeduction > 0 ? `-${preview.lunchDeduction.toFixed(2)}h` : '0.00h'} />
                    <PreviewStat label="Work" value={`${preview.workHours.toFixed(2)}h`} />
                    <PreviewStat label="OT" value={`${preview.otHours.toFixed(2)}h`} highlight />
                  </div>
                ) : (
                  <p className="mt-2 text-xs text-amber-700">{preview.message}</p>
                )}
                <p className="mt-2 text-xs text-slate-500">
                  Standard work hours: {standardHours}h
                  {selectedEmployee ? ` · ${selectedEmployee.fullName}` : ''}
                </p>
              </div>
            </div>
          )}
          <FormField label="Site / Project">
            <input
              {...register('siteProject')}
              className="input"
              list="site-project-options"
              placeholder="Type or select site / project"
            />
            <datalist id="site-project-options">
              {siteProjects.map(x => <option key={x.id} value={x.name} />)}
            </datalist>
          </FormField>
          <FormField label="Transport (one-way trip)">
            <input
              {...register('transport')}
              className="input"
              list="transport-options"
              placeholder="Type or select transport"
            />
            <datalist id="transport-options">
              {transports.map(x => <option key={x.id} value={x.name} />)}
            </datalist>
            <p className="mt-1 text-xs text-slate-400">
              Payroll treats this as the per-trip transport amount and calculates the round trip automatically.
            </p>
          </FormField>
        </div>

        <FormField label="Remarks">
          <input {...register('remarks')} className="input" placeholder="Optional notes…" />
        </FormField>

        <p className="text-xs text-slate-500 bg-slate-50 rounded-lg p-3">
          Work hours and OT hours are calculated by the backend from Start / End, with overnight punches auto-detected when End is earlier than Start.
        </p>
      </div>
    </Modal>
  );
}

function normalizeTimeInput(value?: string) {
  if (!value) return value;
  return value.length === 5 ? `${value}:00` : value;
}

function validateAttendanceRange(start?: string, end?: string, isOvernight = false, needsTime = true) {
  if (!needsTime) return true;
  if (!start || !end) return true;
  if (start === end) return false;

  const startMinutes = timeToMinutes(start);
  const endMinutes = timeToMinutes(end);
  const inferredOvernight = endMinutes < startMinutes;

  if (inferredOvernight) {
    return true;
  }

  if (isOvernight) {
    return endMinutes < startMinutes;
  }
  return endMinutes > startMinutes;
}

function isOvernightPunch(start?: string, end?: string) {
  if (!start || !end) return false;
  return timeToMinutes(end) < timeToMinutes(start);
}

type AttendancePreview =
  | {
      valid: true;
      totalHours: number;
      lunchDeduction: number;
      workHours: number;
      otHours: number;
    }
  | {
      valid: false;
      message: string;
    };

function getAttendancePreview(start?: string, end?: string, standardHours = 8, isOvernight = false, needsTime = true): AttendancePreview {
  if (!needsTime) {
    return { valid: false, message: 'Work time preview is only shown for Present / HalfDay records.' };
  }
  if (!start || !end) {
    return { valid: false, message: 'Enter both Start and End to preview working hours and OT.' };
  }
  if (start === end) {
    return { valid: false, message: 'Start and End cannot be the same.' };
  }

  const startMinutes = timeToMinutes(start);
  const endMinutes = timeToMinutes(end);

  if (isOvernight && endMinutes > startMinutes) {
    return { valid: false, message: 'Night shift checkout must be earlier than check-in time on the clock.' };
  }
  if (!isOvernight && endMinutes <= startMinutes) {
    return { valid: false, message: 'End time must be later than Start unless this is a night shift.' };
  }

  const totalHours = isOvernight
    ? (endMinutes - startMinutes + 24 * 60) / 60
    : (endMinutes - startMinutes) / 60;
  const lunchDeduction = totalHours > 6 ? 1 : 0;
  const effective = Math.max(0, totalHours - lunchDeduction);
  const workHours = Math.min(effective, standardHours);
  const otHours = Math.max(0, effective - standardHours);

  return {
    valid: true,
    totalHours,
    lunchDeduction,
    workHours,
    otHours,
  };
}

function timeToMinutes(value: string) {
  const [hours, minutes] = value.split(':').map(Number);
  return (hours * 60) + minutes;
}

function PreviewStat({ label, value, highlight }: { label: string; value: string; highlight?: boolean }) {
  return (
    <div className="rounded-xl bg-white border border-slate-200 px-3 py-2">
      <p className="text-[10px] uppercase tracking-[0.22em] text-slate-400">{label}</p>
      <p className={`mt-1 font-mono text-sm font-semibold ${highlight ? 'text-indigo-600' : 'text-slate-800'}`}>{value}</p>
    </div>
  );
}

// ─── Lookup Manager Modal ─────────────────────────────────────
function LookupManagerModal({
  open, onClose, lookups, onSaved
}: {
  open: boolean;
  onClose: () => void;
  lookups: AttendanceLookup[];
  onSaved: () => void;
}) {
  const siteProjects = lookups
    .filter(x => x.category === 'SiteProject')
    .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name));
  const transports = lookups
    .filter(x => x.category === 'Transport')
    .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name));

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Manage Dropdowns"
      size="lg"
      footer={
        <button className="btn-secondary" onClick={onClose}>Close</button>
      }
    >
      <div className="space-y-5">
        <p className="text-sm text-slate-600">
          Add, rename, or remove values used by the Attendance dropdowns.
        </p>
        <LookupSection
          title="Site / Project"
          category="SiteProject"
          items={siteProjects}
          onSaved={onSaved}
        />
        <LookupSection
          title="Transport"
          category="Transport"
          items={transports}
          onSaved={onSaved}
        />
      </div>
    </Modal>
  );
}

function LookupSection({
  title, category, items, onSaved
}: {
  title: string;
  category: 'SiteProject' | 'Transport';
  items: AttendanceLookup[];
  onSaved: () => void;
}) {
  const [newName, setNewName] = useState('');
  const [drafts, setDrafts] = useState<Record<number, AttendanceLookup>>({});
  const [busyId, setBusyId] = useState<number | null>(null);

  useEffect(() => {
    const next: Record<number, AttendanceLookup> = {};
    items.forEach(item => { next[item.id] = item; });
    setDrafts(next);
  }, [items]);

  const handleAdd = async () => {
    const name = newName.trim();
    if (!name) return;
    setBusyId(-1);
    try {
      await attendanceLookupApi.create({
        category,
        name,
        isActive: true,
        sortOrder: items.length + 1,
      });
      setNewName('');
      toast.success(`${title} added`);
      onSaved();
    } catch (err: any) {
      toast.error(err?.response?.data?.message || `Failed to add ${title.toLowerCase()}`);
    } finally {
      setBusyId(null);
    }
  };

  const handleSave = async (id: number) => {
    const draft = drafts[id];
    if (!draft) return;
    setBusyId(id);
    try {
      await attendanceLookupApi.update(id, {
        category,
        name: draft.name.trim(),
        isActive: draft.isActive,
        sortOrder: draft.sortOrder,
      });
      toast.success(`${title} updated`);
      onSaved();
    } catch (err: any) {
      toast.error(err?.response?.data?.message || `Failed to update ${title.toLowerCase()}`);
    } finally {
      setBusyId(null);
    }
  };

  const handleDelete = async (id: number, name: string) => {
    if (!window.confirm(`Delete "${name}"?`)) return;
    setBusyId(id);
    try {
      await attendanceLookupApi.delete(id);
      toast.success(`${title} deleted`);
      onSaved();
    } catch (err: any) {
      toast.error(err?.response?.data?.message || `Failed to delete ${title.toLowerCase()}`);
    } finally {
      setBusyId(null);
    }
  };

  return (
    <div className="border border-slate-200 rounded-xl p-4">
      <div className="flex items-center justify-between mb-3">
        <h3 className="font-semibold text-slate-800">{title}</h3>
        <span className="text-xs text-slate-400">{items.length} item(s)</span>
      </div>

      <div className="flex gap-2 mb-4">
        <input
          className="input flex-1"
          value={newName}
          onChange={e => setNewName(e.target.value)}
          placeholder={`Add new ${title.toLowerCase()}`}
        />
        <button className="btn-primary" onClick={handleAdd} disabled={busyId === -1}>
          {busyId === -1 ? 'Adding…' : 'Add'}
        </button>
      </div>

      <div className="space-y-2 max-h-72 overflow-y-auto pr-1">
        {items.length === 0 ? (
          <div className="text-sm text-slate-400 py-4 text-center">No values yet</div>
        ) : items.map(item => {
          const draft = drafts[item.id] ?? item;
          const saving = busyId === item.id;
          return (
            <div key={item.id} className="grid grid-cols-[1fr_88px_80px_auto] gap-2 items-center bg-slate-50 rounded-lg p-2">
              <input
                className="input"
                value={draft.name}
                onChange={e => setDrafts(prev => ({
                  ...prev,
                  [item.id]: { ...draft, name: e.target.value },
                }))}
              />
              <input
                type="number"
                className="input"
                value={draft.sortOrder}
                onChange={e => setDrafts(prev => ({
                  ...prev,
                  [item.id]: { ...draft, sortOrder: Number(e.target.value) || 0 },
                }))}
              />
              <label className="flex items-center gap-2 text-xs text-slate-600 justify-center">
                <input
                  type="checkbox"
                  checked={draft.isActive}
                  onChange={e => setDrafts(prev => ({
                    ...prev,
                    [item.id]: { ...draft, isActive: e.target.checked },
                  }))}
                />
                Active
              </label>
              <div className="flex items-center gap-1 justify-end">
                <button
                  className="btn-secondary px-3 py-2 text-xs"
                  onClick={() => handleSave(item.id)}
                  disabled={saving}
                >
                  {saving ? 'Saving…' : 'Save'}
                </button>
                <button
                  className="btn-danger px-3 py-2 text-xs"
                  onClick={() => handleDelete(item.id, item.name)}
                  disabled={saving}
                >
                  Delete
                </button>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

function PlusIcon() {
  return <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg>;
}

function EditIcon() {
  return <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>;
}

function TrashIcon() {
  return <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/></svg>;
}

function DropdownIcon() {
  return <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M7 10l5 5 5-5"/><path d="M7 6h10a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2z"/></svg>;
}

function InfoPair({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div>
      <p className="text-[10px] uppercase tracking-[0.22em] text-slate-400">{label}</p>
      <p className={`mt-1 ${mono ? 'font-mono text-slate-700' : 'text-slate-700'}`}>{value}</p>
    </div>
  );
}
