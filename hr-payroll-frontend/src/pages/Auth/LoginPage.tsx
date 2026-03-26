import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import toast from 'react-hot-toast';
import { authApi } from '../../api';
import { useAuthStore } from '../../store/authStore';
import { Spinner } from '../../components/ui';
import type { LoginRequest } from '../../types';

export default function LoginPage() {
  const navigate = useNavigate();
  const login = useAuthStore(s => s.login);
  const [loading, setLoading] = useState(false);

  const { register, handleSubmit, formState: { errors } } = useForm<LoginRequest>({
    defaultValues: { username: 'admin', password: 'Admin@123' }
  });

  const onSubmit = async (data: LoginRequest) => {
    setLoading(true);
    try {
      const res = await authApi.login(data);
      login(res);
      toast.success(`Welcome back, ${res.username}!`);
      navigate('/dashboard');
    } catch {
      toast.error('Invalid username or password');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen relative overflow-hidden flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_left,_rgba(30,58,95,0.18),_transparent_36%),radial-gradient(circle_at_bottom_right,_rgba(14,165,233,0.16),_transparent_30%),linear-gradient(135deg,#08111e_0%,#0f2036_48%,#13263f_100%)]" />
      <div className="absolute inset-0 opacity-30 pointer-events-none bg-[linear-gradient(rgba(255,255,255,0.05)_1px,transparent_1px),linear-gradient(90deg,rgba(255,255,255,0.05)_1px,transparent_1px)] bg-[size:72px_72px]" />
      <div className="absolute -top-24 -right-24 w-80 h-80 bg-sky-400/20 rounded-full blur-3xl pointer-events-none" />
      <div className="absolute -bottom-28 -left-16 w-96 h-96 bg-white/10 rounded-full blur-3xl pointer-events-none" />

      <div className="relative w-full max-w-6xl grid lg:grid-cols-[1.15fr_0.85fr] gap-6 items-stretch">
        <section className="hidden lg:flex flex-col justify-between text-white rounded-[2rem] p-8 xl:p-10 bg-white/8 backdrop-blur-sm border border-white/10 shadow-[0_30px_100px_-40px_rgba(8,15,28,0.85)]">
          <div>
            <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-white/10 border border-white/10 text-[11px] font-semibold uppercase tracking-[0.24em] text-white/80 mb-6">
              HR Payroll Full Stack
            </div>
            <h1 className="text-4xl xl:text-5xl font-black leading-[0.95] tracking-tight max-w-xl">
              Payroll, attendance, and payment reports in one clean workspace.
            </h1>
            <p className="mt-5 text-sm xl:text-base text-white/70 max-w-xl leading-7">
              Manage staff profiles, process payroll from backend calculations, and export payment vouchers without leaving the dashboard.
            </p>
          </div>

          <div className="grid grid-cols-3 gap-4 mt-10">
            <div className="rounded-2xl border border-white/10 bg-white/8 p-4">
              <p className="text-[10px] uppercase tracking-[0.24em] text-white/40">Payroll</p>
              <p className="mt-2 text-2xl font-bold">Backend</p>
              <p className="text-xs text-white/60 mt-1">Calculation only</p>
            </div>
            <div className="rounded-2xl border border-white/10 bg-white/8 p-4">
              <p className="text-[10px] uppercase tracking-[0.24em] text-white/40">Profiles</p>
              <p className="mt-2 text-2xl font-bold">Multi</p>
              <p className="text-xs text-white/60 mt-1">Primary + slips</p>
            </div>
            <div className="rounded-2xl border border-white/10 bg-white/8 p-4">
              <p className="text-[10px] uppercase tracking-[0.24em] text-white/40">Reports</p>
              <p className="mt-2 text-2xl font-bold">PDF / XLSX</p>
              <p className="text-xs text-white/60 mt-1">Ready to export</p>
            </div>
          </div>

          <div className="mt-8 flex items-center gap-3 text-white/70 text-sm">
            <span className="inline-flex h-2.5 w-2.5 rounded-full bg-emerald-400 shadow-[0_0_0_6px_rgba(74,222,128,0.12)]" />
            Secure login, live data, and clean reporting flows.
          </div>
        </section>

        <div className="relative w-full max-w-xl lg:ml-auto">
          <div className="bg-white/92 backdrop-blur-xl rounded-[2rem] shadow-[0_35px_90px_-35px_rgba(8,15,28,0.8)] overflow-hidden border border-white/70">
            <div className="bg-gradient-to-r from-navy-800 via-navy-700 to-sky-700 px-8 py-7 text-white">
              <div className="flex items-center gap-3 mb-3">
                <div className="w-10 h-10 bg-white/15 rounded-2xl flex items-center justify-center ring-1 ring-white/15">
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="2.5">
                    <rect x="2" y="3" width="20" height="14" rx="2" />
                    <path d="M8 21h8M12 17v4" />
                  </svg>
                </div>
                <div>
                  <p className="font-bold text-base leading-tight">HR Payroll System</p>
                  <p className="text-white/60 text-xs uppercase tracking-[0.2em]">Management Platform</p>
                </div>
              </div>
              <p className="text-white/75 text-sm leading-6 max-w-md">
                Sign in to manage attendance, process payroll, and export payment reports.
              </p>
            </div>

            <div className="px-8 py-7">
              <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
                <div>
                  <label className="label">Username</label>
                  <input
                    {...register('username', { required: 'Username is required' })}
                    className="input"
                    placeholder="Enter username"
                    autoFocus
                  />
                  {errors.username && <p className="mt-1 text-xs text-red-500">{errors.username.message}</p>}
                </div>
                <div>
                  <label className="label">Password</label>
                  <input
                    {...register('password', { required: 'Password is required' })}
                    type="password"
                    className="input"
                    placeholder="Enter password"
                  />
                  {errors.password && <p className="mt-1 text-xs text-red-500">{errors.password.message}</p>}
                </div>
                <button type="submit" className="btn-primary w-full justify-center py-2.5" disabled={loading}>
                  {loading ? <><Spinner size="sm" /> Signing in…</> : 'Sign in'}
                </button>
              </form>
              <div className="mt-6 rounded-2xl border border-slate-200/80 bg-slate-50/80 p-4">
                <p className="text-[10px] uppercase tracking-[0.24em] text-slate-400 mb-2">Default access</p>
                <p className="text-sm font-mono text-slate-700">admin / Admin@123</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
