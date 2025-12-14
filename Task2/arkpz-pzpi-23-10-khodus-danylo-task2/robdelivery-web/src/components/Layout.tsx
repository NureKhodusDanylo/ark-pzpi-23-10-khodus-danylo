import React, { ReactNode } from 'react';
import { motion } from 'framer-motion';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import {
  Bot,
  LayoutDashboard,
  Package,
  PlusCircle,
  Settings,
  LogOut,
  User,
  Shield,
  Map,
} from 'lucide-react';
import './Layout.css';

interface LayoutProps {
  children: ReactNode;
}

const Layout = ({ children }: LayoutProps) => {
  const navigate = useNavigate();
  const location = useLocation();
  const { user, logout } = useAuthStore();

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  const navItems = [
    { path: '/dashboard', icon: <LayoutDashboard size={20} />, label: 'Dashboard' },
    { path: '/orders', icon: <Package size={20} />, label: 'Orders' },
    { path: '/orders/create', icon: <PlusCircle size={20} />, label: 'New Order' },
    { path: '/robots', icon: <Bot size={20} />, label: 'Robots' },
    { path: '/map', icon: <Map size={20} />, label: 'Live Map' },
    { path: '/profile', icon: <User size={20} />, label: 'Profile' },
  ];

  if (user?.role === 'Admin') {
    navItems.push({ path: '/admin', icon: <Shield size={20} />, label: 'Admin Panel' });
  }

  return (
    <div className="layout">
      <motion.aside
        className="sidebar"
        initial={{ x: -280 }}
        animate={{ x: 0 }}
        transition={{ duration: 0.5 }}
      >
        <div className="sidebar-header">
          <div className="logo">
            <Bot size={32} className="logo-icon" />
            <span className="gradient-text">RobDelivery</span>
          </div>
        </div>

        <nav className="sidebar-nav">
          {navItems.map((item) => (
            <motion.button
              key={item.path}
              onClick={() => navigate(item.path)}
              className={`nav-item ${location.pathname === item.path ? 'active' : ''}`}
              whileHover={{ x: 5 }}
              whileTap={{ scale: 0.98 }}
            >
              {item.icon}
              <span>{item.label}</span>
            </motion.button>
          ))}
        </nav>

        <div className="sidebar-footer">
          <div className="user-info">
            <div className="user-avatar">
              {user?.userName.charAt(0).toUpperCase()}
            </div>
            <div className="user-details">
              <p className="user-name">{user?.userName}</p>
              <p className="user-role">{user?.role}</p>
            </div>
          </div>
          <button onClick={handleLogout} className="logout-btn">
            <LogOut size={20} />
          </button>
        </div>
      </motion.aside>

      <main className="main-content">
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5 }}
        >
          {children}
        </motion.div>
      </main>
    </div>
  );
};

export default Layout;
