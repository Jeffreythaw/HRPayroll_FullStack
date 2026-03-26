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
    <div className="min-h-screen bg-gradient-to-br from-navy-900 via-navy-800 to-slate-800 flex items-center justify-center p-4">
      {/* Background pattern */}
      <div className="absolute inset-0 overflow-hidden pointer-events-none">
        <div className="absolute -top-40 -right-40 w-96 h-96 bg-white/5 rounded-full blur-3xl" />
        <div className="absolute -bottom-40 -left-40 w-96 h-96 bg-white/5 rounded-full blur-3xl" />
      </div>

      <div className="relative w-full max-w-sm">
        {/* Card */}
        <div className="bg-white rounded-2xl shadow-2xl overflow-hidden">
          {/* Header stripe */}
          <div className="bg-navy-800 px-8 py-7">
            <div className="flex items-center gap-3 mb-3">
              <div className="w-9 h-9 bg-white/20 rounded-xl flex items-center justify-center">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="2.5">
                  <rect x="2" y="3" width="20" height="14" rx="2"/>
                  <path d="M8 21h8M12 17v4"/>
                </svg>
              </div>
              <div>
                <p className="text-white font-bold text-base leading-tight">HR Payroll System</p>
                <p className="text-white/50 text-xs">Management Platform</p>
              </div>
            </div>
            <p className="text-white/70 text-sm">Sign in to continue</p>
          </div>

          {/* Form */}
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
            <p className="mt-5 text-center text-xs text-slate-400">
              Default: <span className="font-mono text-slate-600">admin / Admin@123</span>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
