import React, { useState } from 'react';
import './Login.css';

const Login: React.FC = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    console.log('Login attempt:', { email, password });
  };

  return (
    <div className="login-container">
      {/* Sidebar matching the dashboard */}
      <aside className="login-sidebar">
        <div className="sidebar-header">
          <div className="logo-circle">T</div>
          <div className="brand-info">
            <h1 className="brand-name">Travyle</h1>
            <p className="brand-subtitle">Admin console</p>
          </div>
        </div>
        
        <div className="sidebar-decorative">
          <div className="decorative-line"></div>
          <p className="decorative-text">Secure Access Portal</p>
        </div>
      </aside>

      {/* Main Content Area */}
      <main className="login-main">
        <div className="login-card-container">
          <div className="login-header">
            <h2>Welcome back</h2>
            <p>Please sign in to access the admin console.</p>
          </div>

          <form className="login-form" onSubmit={handleSubmit}>
            <div className="form-group">
              <label htmlFor="email">Email address</label>
              <input
                type="email"
                id="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="admin@travyle.io"
                required
              />
            </div>
            
            <div className="form-group">
              <label htmlFor="password">Password</label>
              <input
                type="password"
                id="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                required
              />
            </div>

            <div className="form-options">
              <label className="remember-me">
                <input type="checkbox" />
                <span>Remember me</span>
              </label>
              <a href="#" className="forgot-password">Forgot password?</a>
            </div>

            <button type="submit" className="login-button">
              Sign In to Console
            </button>
          </form>
        </div>
      </main>
    </div>
  );
};

export default Login;
