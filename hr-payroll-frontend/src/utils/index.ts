export const MONTHS = [
  'January','February','March','April','May','June',
  'July','August','September','October','November','December'
];

export function formatCurrency(v: number) {
  return new Intl.NumberFormat('en-SG', { style: 'currency', currency: 'SGD', minimumFractionDigits: 2 }).format(v);
}

export function formatDate(d?: string | null) {
  if (!d) return '—';
  return new Date(d).toLocaleDateString('en-SG', { day: '2-digit', month: 'short', year: 'numeric' });
}

export function formatTime(t?: string) {
  if (!t) return '—';
  return t.slice(0, 5);
}

export function currentMonthYear() {
  const now = new Date();
  return { month: now.getMonth() + 1, year: now.getFullYear() };
}

export function statusBadgeClass(status: string) {
  switch (status) {
    case 'Present':  return 'badge-green';
    case 'Absent':   return 'badge-red';
    case 'Leave':    return 'badge-amber';
    case 'Holiday':  return 'badge-purple';
    case 'HalfDay':  return 'badge-blue';
    case 'Active':   return 'badge-green';
    case 'Inactive': return 'badge-gray';
    case 'Approved': return 'badge-green';
    case 'Paid':     return 'badge-blue';
    case 'Draft':    return 'badge-gray';
    default:         return 'badge-gray';
  }
}

export function attendanceStatuses() {
  return ['Present', 'Absent', 'Leave', 'Holiday', 'HalfDay'];
}

export function payrollStatuses() {
  return ['Draft', 'Approved', 'Paid'];
}
