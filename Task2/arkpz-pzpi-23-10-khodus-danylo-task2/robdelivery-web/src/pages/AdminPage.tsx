import React from 'react';
import { motion } from 'framer-motion';
import { useQuery } from '@tanstack/react-query';
import {
  Users,
  Package,
  Bot,
  MapPin,
  TrendingUp,
  DollarSign,
  CheckCircle,
  XCircle,
  Download,
  Database,
} from 'lucide-react';
import Layout from '../components/Layout';
import { adminAPI } from '../lib/api';
import './AdminPage.css';

const AdminPage = () => {
  const { data: stats, isLoading } = useQuery({
    queryKey: ['admin', 'stats'],
    queryFn: async () => {
      const response = await adminAPI.getStats();
      return response.data;
    },
  });

  const { data: efficiency } = useQuery({
    queryKey: ['admin', 'efficiency'],
    queryFn: async () => {
      const response = await adminAPI.getRobotEfficiency();
      return response.data;
    },
  });

  const handleExport = async () => {
    try {
      const response = await adminAPI.exportDeliveryHistory();
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', 'delivery-history.json');
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (error) {
      alert('Failed to export delivery history');
    }
  };

  const handleBackup = async () => {
    try {
      await adminAPI.createBackup();
      alert('Backup created successfully');
    } catch (error) {
      alert('Failed to create backup');
    }
  };

  const mainStats = stats ? [
    { icon: <Users size={28} />, label: 'Total Users', value: stats.totalUsers, color: 'primary' },
    { icon: <Package size={28} />, label: 'Total Orders', value: stats.totalOrders, color: 'secondary' },
    { icon: <Bot size={28} />, label: 'Total Robots', value: stats.totalRobots, color: 'accent' },
    { icon: <MapPin size={28} />, label: 'Total Nodes', value: stats.totalNodes, color: 'success' },
  ] : [];

  const orderStats = stats ? [
    { icon: <TrendingUp size={24} />, label: 'Active', value: stats.activeOrders, color: 'warning' },
    { icon: <CheckCircle size={24} />, label: 'Completed', value: stats.completedOrders, color: 'success' },
    { icon: <XCircle size={24} />, label: 'Cancelled', value: stats.cancelledOrders, color: 'error' },
  ] : [];

  const robotStats = stats ? [
    { icon: <Bot size={24} />, label: 'Available', value: stats.availableRobots, color: 'success' },
    { icon: <TrendingUp size={24} />, label: 'Busy', value: stats.busyRobots, color: 'primary' },
    { icon: <TrendingUp size={24} />, label: 'Charging', value: stats.chargingRobots, color: 'warning' },
    { icon: <TrendingUp size={24} />, label: 'Avg Battery', value: `${stats.averageBatteryLevel.toFixed(1)}%`, color: 'info' },
  ] : [];

  return (
    <Layout>
      <div className="admin-page">
        <motion.div
          className="page-header"
          initial={{ opacity: 0, y: -20 }}
          animate={{ opacity: 1, y: 0 }}
        >
          <div>
            <h1 className="page-title">
              Admin <span className="gradient-text">Dashboard</span>
            </h1>
            <p className="page-subtitle">System overview and analytics</p>
          </div>
          <div className="admin-actions">
            <button onClick={handleExport} className="btn btn-secondary">
              <Download size={20} />
              Export Data
            </button>
            <button onClick={handleBackup} className="btn btn-primary">
              <Database size={20} />
              Create Backup
            </button>
          </div>
        </motion.div>

        {isLoading ? (
          <div className="loading-state">
            <div className="spinner"></div>
            <p>Loading statistics...</p>
          </div>
        ) : (
          <>
            <div className="stats-section">
              <h2 className="section-heading">System Overview</h2>
              <div className="main-stats-grid">
                {mainStats.map((stat, index) => (
                  <motion.div
                    key={index}
                    className={`admin-stat-card stat-${stat.color}`}
                    initial={{ opacity: 0, y: 20 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: index * 0.1 }}
                    whileHover={{ y: -5 }}
                  >
                    <div className="stat-icon-large">{stat.icon}</div>
                    <div className="stat-details">
                      <p className="stat-label">{stat.label}</p>
                      <h2 className="stat-value-large">{stat.value}</h2>
                    </div>
                  </motion.div>
                ))}
              </div>
            </div>

            <div className="analytics-grid">
              <motion.div
                className="analytics-card"
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.4 }}
              >
                <h3 className="card-title">Order Statistics</h3>
                <div className="mini-stats">
                  {orderStats.map((stat, index) => (
                    <div key={index} className="mini-stat">
                      <div className={`mini-stat-icon stat-${stat.color}`}>
                        {stat.icon}
                      </div>
                      <div>
                        <p className="mini-stat-label">{stat.label}</p>
                        <h4 className="mini-stat-value">{stat.value}</h4>
                      </div>
                    </div>
                  ))}
                </div>
              </motion.div>

              <motion.div
                className="analytics-card"
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.5 }}
              >
                <h3 className="card-title">Robot Fleet</h3>
                <div className="mini-stats">
                  {robotStats.map((stat, index) => (
                    <div key={index} className="mini-stat">
                      <div className={`mini-stat-icon stat-${stat.color}`}>
                        {stat.icon}
                      </div>
                      <div>
                        <p className="mini-stat-label">{stat.label}</p>
                        <h4 className="mini-stat-value">{stat.value}</h4>
                      </div>
                    </div>
                  ))}
                </div>
              </motion.div>
            </div>

            {stats && (
              <motion.div
                className="revenue-card"
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.6 }}
              >
                <h3 className="card-title">
                  <DollarSign size={24} />
                  Revenue Overview
                </h3>
                <div className="revenue-stats">
                  <div className="revenue-item">
                    <span className="revenue-label">Delivery Revenue</span>
                    <span className="revenue-value">${stats.deliveryRevenue.toFixed(2)}</span>
                  </div>
                  <div className="revenue-item">
                    <span className="revenue-label">Product Revenue</span>
                    <span className="revenue-value">${stats.productRevenue.toFixed(2)}</span>
                  </div>
                  <div className="revenue-item total">
                    <span className="revenue-label">Total Revenue</span>
                    <span className="revenue-value gradient-text">
                      ${stats.totalRevenue.toFixed(2)}
                    </span>
                  </div>
                </div>
              </motion.div>
            )}

            {efficiency && efficiency.length > 0 && (
              <motion.div
                className="efficiency-card"
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.7 }}
              >
                <h3 className="card-title">Robot Efficiency Rankings</h3>
                <div className="efficiency-list">
                  {efficiency
                    .sort((a, b) => b.efficiencyScore - a.efficiencyScore)
                    .slice(0, 10)
                    .map((robot, index) => (
                      <div key={robot.robotId} className="efficiency-item">
                        <div className="efficiency-rank">#{index + 1}</div>
                        <div className="efficiency-details">
                          <span className="efficiency-name">{robot.serialNumber}</span>
                          <div className="efficiency-meta">
                            <span>{robot.completedOrders} orders</span>
                            <span>•</span>
                            <span>{robot.batteryLevel}% battery</span>
                          </div>
                        </div>
                        <div className="efficiency-score">
                          <div className="score-bar">
                            <div
                              className="score-fill"
                              style={{ width: `${robot.efficiencyScore}%` }}
                            />
                          </div>
                          <span className="score-text">{robot.efficiencyScore.toFixed(1)}</span>
                        </div>
                      </div>
                    ))}
                </div>
              </motion.div>
            )}
          </>
        )}
      </div>
    </Layout>
  );
};

export default AdminPage;
