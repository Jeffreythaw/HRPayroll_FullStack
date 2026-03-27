import { useEffect, useState } from 'react';
import toast from 'react-hot-toast';
import { employeeProfileApi, openSaveFileHandle, reportApi, saveBlobFallback, saveBlobToHandle } from '../../api';
import { PageHeader, MonthYearPicker, Spinner } from '../../components/ui';
import { currentMonthYear, MONTHS } from '../../utils';
import type { EmployeePayrollProfile } from '../../types';

export default function ReportsPage() {
  const { month, year } = currentMonthYear();
  const [selMonth, setSelMonth] = useState(month);
  const [selYear, setSelYear] = useState(year);
  const [profiles, setProfiles] = useState<EmployeePayrollProfile[]>([]);
  const [selectedProfileIds, setSelectedProfileIds] = useState<number[]>([]);
  const [search, setSearch] = useState('');
  const [generating, setGenerating] = useState(false);
  const [loadingProfiles, setLoadingProfiles] = useState(true);

  useEffect(() => {
    employeeProfileApi.getAll({ status: 'Active' })
      .then(setProfiles)
      .finally(() => setLoadingProfiles(false));
  }, []);

  const toggle = (id: number) =>
    setSelectedProfileIds(prev => prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id]);

  const toggleAll = () =>
    setSelectedProfileIds(prev => prev.length === filteredProfiles.length ? [] : filteredProfiles.map(p => p.id));

  const filteredProfiles = profiles.filter(profile => {
    if (!search) return true;
    const query = search.toLowerCase();
    return (
      profile.employeeName.toLowerCase().includes(query) ||
      profile.employeeCode.toLowerCase().includes(query) ||
      profile.profileName.toLowerCase().includes(query)
    );
  });

  const buildRequest = () => ({
    month: selMonth,
    year: selYear,
    profileIds: selectedProfileIds.length > 0
      ? selectedProfileIds
      : filteredProfiles.map(profile => profile.id),
  });

  const handleGenerateExcel = async () => {
    setGenerating(true);
    try {
      const filename = `Payroll_Report_${monthName}_${selYear}.xlsx`;
      const saveHandle = await openSaveFileHandle(filename);
      const blob = await reportApi.fetchExcelBlob(buildRequest());
      if (saveHandle) {
        await saveBlobToHandle(blob, saveHandle);
      } else {
        await saveBlobFallback(blob, filename);
      }
      const count = selectedProfileIds.length || filteredProfiles.length;
      toast.success(`Excel report saved for ${count} profile(s)`);
    } catch (err) {
      if ((err as { name?: string })?.name === 'AbortError') return;
      toast.error('Failed to generate report');
    } finally {
      setGenerating(false);
    }
  };

  const handleGenerateVoucher = async () => {
    setGenerating(true);
    try {
      const filename = `Payment_Voucher_${monthName}_${selYear}.pdf`;
      const saveHandle = await openSaveFileHandle(filename);
      const blob = await reportApi.fetchPaymentVoucherBlob(buildRequest());
      if (saveHandle) {
        await saveBlobToHandle(blob, saveHandle);
      } else {
        await saveBlobFallback(blob, filename);
      }
      const count = selectedProfileIds.length || filteredProfiles.length;
      toast.success(`Payment voucher saved for ${count} profile(s)`);
    } catch (err) {
      if ((err as { name?: string })?.name === 'AbortError') return;
      toast.error('Failed to generate payment voucher');
    } finally {
      setGenerating(false);
    }
  };

  const monthName = MONTHS[selMonth - 1];

  return (
    <div>
      <PageHeader
        title="Reports"
        subtitle="Generate payroll reports and payment vouchers for download"
      />

      <div className="grid grid-cols-1 xl:grid-cols-3 gap-4">
        {/* Config panel */}
        <div className="xl:col-span-2 space-y-4">
          {/* Period */}
          <div className="card p-5">
            <h2 className="text-sm font-semibold text-slate-700 mb-4 flex items-center gap-2">
              <CalendarIcon /> Report Period
            </h2>
            <MonthYearPicker month={selMonth} year={selYear} onChange={(m, y) => { setSelMonth(m); setSelYear(y); }} />
            <p className="text-xs text-slate-400 mt-2">
              Generating report for: <strong className="text-slate-600">{monthName} {selYear}</strong>
            </p>
          </div>

          {/* Profile selection */}
          <div className="card p-5">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-sm font-semibold text-slate-700 flex items-center gap-2">
                <PeopleIcon /> Select Payroll Profiles
              </h2>
              <button
                onClick={toggleAll}
                className="text-xs text-navy-800 hover:underline font-medium"
              >
                {selectedProfileIds.length === filteredProfiles.length ? 'Deselect All' : 'Select All'}
              </button>
            </div>

            <input
              value={search}
              onChange={e => setSearch(e.target.value)}
              className="input mb-3"
              placeholder="Search employee or profile…"
            />

            {loadingProfiles ? (
              <div className="flex justify-center py-8"><Spinner /></div>
            ) : (
              <div className="max-h-72 overflow-y-auto space-y-1">
                {filteredProfiles.map(profile => (
                  <label
                    key={profile.id}
                    className={`flex items-center gap-3 p-2.5 rounded-lg cursor-pointer transition-colors ${
                      selectedProfileIds.includes(profile.id) ? 'bg-navy-50 border border-navy-200' : 'hover:bg-slate-50 border border-transparent'
                    }`}
                  >
                    <input
                      type="checkbox"
                      checked={selectedProfileIds.includes(profile.id)}
                      onChange={() => toggle(profile.id)}
                      className="rounded accent-navy-800"
                    />
                    <div className="w-7 h-7 bg-navy-100 text-navy-800 rounded-full flex items-center justify-center text-xs font-bold flex-shrink-0">
                      {profile.employeeName.split(' ').map(part => part[0]).slice(0, 2).join('')}
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium text-slate-900">{profile.employeeName}</p>
                      <p className="text-xs text-slate-400">
                        {profile.employeeCode} · {profile.profileName} · {profile.salaryMode}
                      </p>
                    </div>
                    <span className="text-xs font-mono text-slate-400">
                      {profile.isPrimary ? 'Primary' : 'Slip'}
                    </span>
                  </label>
                ))}
              </div>
            )}

            <p className="text-xs text-slate-400 mt-3">
              {selectedProfileIds.length === 0
                ? `All ${filteredProfiles.length} visible profiles will be included`
                : `${selectedProfileIds.length} profile${selectedProfileIds.length !== 1 ? 's' : ''} selected`}
            </p>
          </div>
        </div>

        {/* Preview panel + generate */}
        <div className="space-y-4">
          {/* Summary */}
          <div className="card p-5">
            <h2 className="text-sm font-semibold text-slate-700 mb-4 flex items-center gap-2">
              <FileIcon /> Report Preview
            </h2>
            <div className="space-y-3">
              <div className="bg-slate-50 rounded-lg p-3 text-sm">
                <p className="text-slate-500 text-xs uppercase font-semibold mb-2">Output Format</p>
                <div className="space-y-3">
                  <div className="flex items-center gap-2">
                    <div className="w-8 h-8 bg-emerald-600 text-white rounded flex items-center justify-center">
                      <ExcelIcon />
                    </div>
                    <div>
                      <p className="font-semibold text-slate-800 text-xs">Microsoft Excel (.xlsx)</p>
                      <p className="text-xs text-slate-400">Payroll workbook by profile slip</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <div className="w-8 h-8 bg-navy-800 text-white rounded flex items-center justify-center">
                      <PdfIcon />
                    </div>
                    <div>
                      <p className="font-semibold text-slate-800 text-xs">Payment Voucher (.pdf)</p>
                      <p className="text-xs text-slate-400">Per-profile printable voucher</p>
                    </div>
                  </div>
                </div>
              </div>

              <div className="space-y-2 text-xs text-slate-600">
                <div className="flex justify-between">
                  <span className="text-slate-500">Period</span>
                  <span className="font-semibold">{monthName} {selYear}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-500">Profiles</span>
                  <span className="font-semibold">
                    {selectedProfileIds.length === 0 ? filteredProfiles.length : selectedProfileIds.length}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-500">Worksheets</span>
                  <span className="font-semibold">
                    Employee name, profile suffix if needed
                  </span>
                </div>
              </div>

              <div className="border-t border-slate-200 pt-3 space-y-1.5 text-xs text-slate-500">
                <p className="font-semibold text-slate-600 mb-1">Each sheet includes:</p>
                {[
                  'Employee information header',
                  'Daily attendance table',
                  'Work hours & OT breakdown',
                  'Payroll summary with calculations',
                  'Net salary highlighted row',
                ].map(item => (
                  <div key={item} className="flex items-center gap-1.5">
                    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="#059669" strokeWidth="2.5">
                      <polyline points="20 6 9 17 4 12"/>
                    </svg>
                    {item}
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* Generate button */}
          <div className="grid grid-cols-1 gap-3">
            <button
              className="btn-success w-full justify-center py-3 text-base font-semibold"
              onClick={handleGenerateExcel}
              disabled={generating}
            >
              {generating
                ? <><Spinner size="sm" /> Generating…</>
                : <><DownloadIcon /> Generate Excel</>}
            </button>

            <button
              className="w-full justify-center py-3 text-base font-semibold rounded-xl border border-navy-200 text-navy-800 bg-white hover:bg-navy-50 transition-colors disabled:opacity-60 disabled:cursor-not-allowed flex items-center gap-2"
              onClick={handleGenerateVoucher}
              disabled={generating}
            >
              {generating
                ? <><Spinner size="sm" /> Generating…</>
                : <><PdfIcon /> Generate Payment Voucher</>}
            </button>
          </div>

          <p className="text-xs text-center text-slate-400">
            Supported browsers will ask where to save the file. Otherwise it will download to your default Downloads folder.
          </p>
        </div>
      </div>
    </div>
  );
}

function CalendarIcon() { return <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><rect x="3" y="4" width="18" height="18" rx="2"/><line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/></svg>; }
function PeopleIcon()   { return <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/></svg>; }
function FileIcon()     { return <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>; }
function DownloadIcon() { return <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>; }
function ExcelIcon()    { return <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>; }
function PdfIcon()      { return <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><path d="M8 13h8"/><path d="M8 17h6"/></svg>; }
