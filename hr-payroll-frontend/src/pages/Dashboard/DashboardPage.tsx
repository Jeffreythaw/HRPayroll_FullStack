import { useEffect, useState } from 'react';
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Cell
} from 'recharts';
import { dashboardApi } from '../../api';
import { PageHeader, StatCard, Spinner } from '../../components/ui';
import { formatCurrency, formatTime, statusBadgeClass } from '../../utils';
import type { DashboardSummary } from '../../types';

const COLORS = ['#1e3a5f','#2d6a9f','#3b82f6','#6366f1','#8b5cf6','#a78bfa'];

export default function DashboardPage() {
  const [data, setData] = useState<DashboardSummary | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    dashboardApi.getSummary()
      .then(setData)
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Spinner size="lg" />
      </div>
    );
  }

  if (!data) return null;

  const today = new Date().toLocaleDateString('en-SG', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });
  const monthName = new Date().toLocaleString('default', { month: 'long', year: 'numeric' });

  return (
    <div className="space-y-6">
      <PageHeader
        title="Dashboard"
        subtitle={today}
        actions={
          <div className="inline-flex items-center gap-2 rounded-full border border-slate-200 bg-white/80 px-4 py-2 text-xs font-semibold text-slate-600 shadow-sm">
            <span className="inline-flex h-2.5 w-2.5 rounded-full bg-emerald-500" />
            Live backend data
          </div>
        }
      />

      <div className="card p-6 overflow-hidden relative">
        <div className="absolute inset-0 pointer-events-none">
          <div className="absolute -top-20 right-0 w-64 h-64 bg-sky-400/10 rounded-full blur-3xl" />
          <div className="absolute -bottom-24 left-24 w-64 h-64 bg-navy-500/10 rounded-full blur-3xl" />
        </div>
        <div className="relative grid lg:grid-cols-[1.3fr_0.7fr] gap-6 items-center">
          <div>
            <p className="text-[10px] font-bold uppercase tracking-[0.28em] text-slate-400 mb-2">
              Summary
            </p>
            <h2 className="text-2xl lg:text-3xl font-black tracking-tight text-slate-900">
              Operations at a glance
            </h2>
            <p className="mt-3 text-sm text-slate-500 max-w-2xl leading-6">
              Review attendance, department headcount, and payroll totals from the latest backend calculation.
            </p>
            <div className="mt-5 flex flex-wrap gap-3">
              <MiniPill label="Employees" value={String(data.totalEmployees)} />
              <MiniPill label="Active" value={String(data.activeEmployees)} />
              <MiniPill label="Payroll" value={formatCurrency(data.totalPayrollThisMonth)} />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="rounded-2xl border border-slate-100 bg-white/80 p-4">
              <p className="text-[10px] uppercase tracking-[0.24em] text-slate-400">Present Today</p>
              <p className="mt-2 text-2xl font-bold text-slate-900">{data.presentToday}</p>
              <p className="text-xs text-slate-500 mt-1">{data.absentToday} absent</p>
            </div>
            <div className="rounded-2xl border border-slate-100 bg-white/80 p-4">
              <p className="text-[10px] uppercase tracking-[0.24em] text-slate-400">On Leave</p>
              <p className="mt-2 text-2xl font-bold text-slate-900">{data.onLeaveToday}</p>
              <p className="text-xs text-slate-500 mt-1">Today</p>
            </div>
            <div className="rounded-2xl border border-slate-100 bg-white/80 p-4 col-span-2">
              <p className="text-[10px] uppercase tracking-[0.24em] text-slate-400">Attendance Rate</p>
              <p className="mt-2 text-3xl font-black text-slate-900">
                {data.activeEmployees > 0 ? `${Math.round((data.presentToday / data.activeEmployees) * 100)}%` : '—'}
              </p>
              <p className="text-xs text-slate-500 mt-1">
                {data.presentToday} of {data.activeEmployees} employees present
              </p>
            </div>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-2 xl:grid-cols-4 gap-4">
        <StatCard
          label="Total Employees"
          value={data.totalEmployees}
          icon={<PeopleIcon />}
          color="bg-navy-800"
          sub={`${data.activeEmployees} active`}
        />
        <StatCard
          label="Present Today"
          value={data.presentToday}
          icon={<CheckIcon />}
          color="bg-emerald-600"
          sub={`${data.absentToday} absent, ${data.onLeaveToday} on leave`}
        />
        <StatCard
          label="On Leave"
          value={data.onLeaveToday}
          icon={<LeaveIcon />}
          color="bg-amber-500"
        />
        <StatCard
          label={`Payroll — ${monthName}`}
          value={formatCurrency(data.totalPayrollThisMonth)}
          icon={<DollarIcon />}
          color="bg-indigo-600"
          sub="Net salary total"
        />
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-5 gap-4">
        <section className="xl:col-span-3 card p-5">
          <div className="flex items-end justify-between gap-4 mb-4">
            <div>
              <p className="text-[10px] font-bold uppercase tracking-[0.24em] text-slate-400">Department</p>
              <h2 className="text-base font-semibold text-slate-900">Headcount by department</h2>
            </div>
            <p className="text-xs text-slate-400">Live snapshot</p>
          </div>
          {data.departmentHeadcounts.length === 0 ? (
            <p className="text-sm text-slate-400 py-10 text-center">No department data</p>
          ) : (
            <ResponsiveContainer width="100%" height={240}>
              <BarChart data={data.departmentHeadcounts} margin={{ top: 8, right: 0, bottom: 0, left: -20 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#eef2f7" vertical={false} />
                <XAxis dataKey="department" tick={{ fontSize: 12, fill: '#64748b' }} />
                <YAxis tick={{ fontSize: 12, fill: '#64748b' }} allowDecimals={false} />
                <Tooltip
                  contentStyle={{ fontSize: 12, borderRadius: 12, border: '1px solid #e2e8f0', boxShadow: '0 20px 40px -25px rgba(15,23,42,0.35)' }}
                  cursor={{ fill: '#f8fafc' }}
                />
                <Bar dataKey="count" radius={[10, 10, 0, 0]}>
                  {data.departmentHeadcounts.map((_, i) => (
                    <Cell key={i} fill={COLORS[i % COLORS.length]} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          )}
        </section>

        <section className="xl:col-span-2 card p-5">
          <div className="flex items-end justify-between gap-4 mb-4">
            <div>
              <p className="text-[10px] font-bold uppercase tracking-[0.24em] text-slate-400">Attendance</p>
              <h2 className="text-base font-semibold text-slate-900">Today's status</h2>
            </div>
            <p className="text-xs text-slate-400">Active employees</p>
          </div>
          <div className="space-y-3">
            {[
              { label: 'Present', value: data.presentToday, color: 'bg-emerald-500' },
              { label: 'Absent',  value: data.absentToday,  color: 'bg-red-500' },
              { label: 'On Leave', value: data.onLeaveToday, color: 'bg-amber-500' },
            ].map(({ label, value, color }) => {
              const total = data.activeEmployees || 1;
              const pct = Math.round((value / total) * 100);
              return (
                <div key={label}>
                  <div className="flex justify-between text-xs mb-1">
                    <span className="text-slate-600 font-medium">{label}</span>
                    <span className="font-semibold text-slate-800">{value}</span>
                  </div>
                  <div className="h-2 bg-slate-100 rounded-full overflow-hidden">
                    <div
                      className={`h-full ${color} rounded-full transition-all duration-700`}
                      style={{ width: `${pct}%` }}
                    />
                  </div>
                </div>
              );
            })}
          </div>
          <div className="mt-5 pt-4 border-t border-slate-100">
            <p className="text-xs text-slate-500 font-medium mb-2">Payroll total</p>
            <p className="text-3xl font-black text-slate-900">{formatCurrency(data.totalPayrollThisMonth)}</p>
            <p className="text-xs text-slate-400 mt-1">Net salary for {monthName}</p>
          </div>
        </section>
      </div>

      <div className="card overflow-hidden">
        <div className="p-5 border-b border-slate-100 flex items-center justify-between gap-3">
          <div>
            <p className="text-[10px] font-bold uppercase tracking-[0.24em] text-slate-400">Recent Activity</p>
            <h2 className="text-base font-semibold text-slate-900">Recent attendance records</h2>
          </div>
          <p className="text-xs text-slate-400">Latest rows</p>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr>
                <th className="table-th">Employee</th>
                <th className="table-th">Date</th>
                <th className="table-th">Start</th>
                <th className="table-th">Status</th>
              </tr>
            </thead>
            <tbody>
              {data.recentAttendances.length === 0 ? (
                <tr>
                  <td colSpan={4} className="table-td text-center text-slate-400 py-10">
                    No attendance records today
                  </td>
                </tr>
              ) : (
                data.recentAttendances.map((a, i) => (
                  <tr key={i} className="hover:bg-slate-50/80 transition-colors">
                    <td className="table-td font-medium">{a.employeeName}</td>
                    <td className="table-td text-slate-500">{a.date}</td>
                    <td className="table-td font-mono text-slate-600">{formatTime(a.start)}</td>
                    <td className="table-td">
                      <span className={statusBadgeClass(a.status)}>{a.status}</span>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

function MiniPill({ label, value }: { label: string; value: string }) {
  return (
    <div className="inline-flex items-center gap-2 rounded-full border border-slate-200 bg-white/75 px-4 py-2 shadow-sm">
      <span className="text-[10px] uppercase tracking-[0.24em] text-slate-400">{label}</span>
      <span className="text-sm font-semibold text-slate-900">{value}</span>
    </div>
  );
}

function PeopleIcon()  { return <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75"/></svg>; }
function CheckIcon()   { return <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5"><polyline points="20 6 9 17 4 12"/></svg>; }
function LeaveIcon()   { return <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><rect x="3" y="4" width="18" height="18" rx="2"/><line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/></svg>; }
function DollarIcon()  { return <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><line x1="12" y1="1" x2="12" y2="23"/><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/></svg>; }
