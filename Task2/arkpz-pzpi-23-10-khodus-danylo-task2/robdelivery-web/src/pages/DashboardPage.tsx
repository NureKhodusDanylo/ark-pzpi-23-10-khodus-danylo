import React from 'react';
import { motion } from 'framer-motion';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import {
  Package,
  Send,
  Inbox,
  TrendingUp,
  Clock,
  CheckCircle,
  XCircle,
  Plus,
  ArrowRight,
} from 'lucide-react';
import Layout from '../components/Layout';
import { orderAPI } from '../lib/api';
import { useAuthStore } from '../store/authStore';
import './DashboardPage.css';

const DashboardPage = () => {
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);

  const { data: myOrders, isLoading } = useQuery({
    queryKey: ['orders', 'my-orders'],
    queryFn: async () => {
      const response = await orderAPI.getMyOrders();
      return response.data;
    },
  });

  const allOrders = myOrders || [];
  const activeOrders = allOrders.filter((o) =>
    ['Pending', 'Processing', 'EnRoute'].includes(o.status)
  );
  const completedOrders = allOrders.filter((o) => o.status === 'Delivered');
  const cancelledOrders = allOrders.filter((o) => o.status === 'Cancelled');

  const stats = [
    {
      icon: <Package size={24} />,
      label: 'Total Orders',
      value: allOrders.length,
      color: 'primary',
    },
    {
      icon: <Clock size={24} />,
      label: 'Active',
      value: activeOrders.length,
      color: 'warning',
    },
    {
      icon: <CheckCircle size={24} />,
      label: 'Completed',
      value: completedOrders.length,
      color: 'success',
    },
    {
      icon: <XCircle size={24} />,
      label: 'Cancelled',
      value: cancelledOrders.length,
      color: 'error',
    },
  ];

  const recentOrders = allOrders
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
    .slice(0, 5);

  const getStatusColor = (status: string) => {
    const colors: Record<string, string> = {
      Pending: 'warning',
      Processing: 'info',
      EnRoute: 'primary',
      Delivered: 'success',
      Cancelled: 'error',
    };
    return colors[status] || 'info';
  };

  return (
    <Layout>
      <div className="dashboard-page">
        <motion.div
          className="dashboard-header"
          initial={{ opacity: 0, y: -20 }}
          animate={{ opacity: 1, y: 0 }}
        >
          <div>
            <h1 className="page-title">
              Welcome back, <span className="gradient-text">{user?.userName}</span>
            </h1>
            <p className="page-subtitle">Here's what's happening with your deliveries</p>
          </div>
          <button
            onClick={() => navigate('/orders/create')}
            className="btn btn-primary"
          >
            <Plus size={20} />
            New Order
          </button>
        </motion.div>

        <div className="stats-grid">
          {stats.map((stat, index) => (
            <motion.div
              key={index}
              className={`stat-card stat-${stat.color}`}
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: index * 0.1 }}
              whileHover={{ y: -5 }}
            >
              <div className="stat-icon">{stat.icon}</div>
              <div className="stat-content">
                <p className="stat-label">{stat.label}</p>
                <h3 className="stat-value">{stat.value}</h3>
              </div>
            </motion.div>
          ))}
        </div>

        <div className="dashboard-content">
          <motion.div
            className="dashboard-section"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.3 }}
          >
            <div className="section-header">
              <h2 className="section-title">Recent Orders</h2>
              <button
                onClick={() => navigate('/orders')}
                className="btn btn-ghost"
              >
                View All <ArrowRight size={16} />
              </button>
            </div>

            {isLoading ? (
              <div className="loading-state">
                <div className="spinner"></div>
                <p>Loading orders...</p>
              </div>
            ) : recentOrders.length === 0 ? (
              <motion.div
                className="empty-state"
                initial={{ opacity: 0, scale: 0.9 }}
                animate={{ opacity: 1, scale: 1 }}
              >
                <Package size={64} className="empty-icon" />
                <h3>No orders yet</h3>
                <p>Create your first delivery order to get started</p>
                <button
                  onClick={() => navigate('/orders/create')}
                  className="btn btn-primary"
                >
                  <Plus size={20} />
                  Create Order
                </button>
              </motion.div>
            ) : (
              <div className="orders-list">
                {recentOrders.map((order, index) => (
                  <motion.div
                    key={order.id}
                    className="order-item"
                    initial={{ opacity: 0, x: -20 }}
                    animate={{ opacity: 1, x: 0 }}
                    transition={{ delay: index * 0.1 }}
                    whileHover={{ x: 5 }}
                    onClick={() => navigate(`/orders`)}
                  >
                    <div className="order-icon">
                      {order.senderId === user?.id ? (
                        <Send size={20} />
                      ) : (
                        <Inbox size={20} />
                      )}
                    </div>
                    <div className="order-details">
                      <h4 className="order-name">{order.name}</h4>
                      <p className="order-description">
                        {order.senderId === user?.id
                          ? `To: ${order.recipientName}`
                          : `From: ${order.senderName}`}
                      </p>
                    </div>
                    <div className="order-meta">
                      <span className={`badge badge-${getStatusColor(order.status)}`}>
                        {order.status}
                      </span>
                      <span className="order-price">${order.deliveryPrice}</span>
                    </div>
                  </motion.div>
                ))}
              </div>
            )}
          </motion.div>

          <motion.div
            className="dashboard-section"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.4 }}
          >
            <div className="section-header">
              <h2 className="section-title">Quick Actions</h2>
            </div>

            <div className="quick-actions">
              <motion.button
                className="quick-action-card"
                whileHover={{ scale: 1.05 }}
                whileTap={{ scale: 0.95 }}
                onClick={() => navigate('/orders/create')}
              >
                <Plus className="action-icon" size={32} />
                <h3>Create Order</h3>
                <p>Send a new package</p>
              </motion.button>

              <motion.button
                className="quick-action-card"
                whileHover={{ scale: 1.05 }}
                whileTap={{ scale: 0.95 }}
                onClick={() => navigate('/orders')}
              >
                <Package className="action-icon" size={32} />
                <h3>My Orders</h3>
                <p>View all deliveries</p>
              </motion.button>

              <motion.button
                className="quick-action-card"
                whileHover={{ scale: 1.05 }}
                whileTap={{ scale: 0.95 }}
                onClick={() => navigate('/robots')}
              >
                <TrendingUp className="action-icon" size={32} />
                <h3>Track Robots</h3>
                <p>See robot status</p>
              </motion.button>
            </div>
          </motion.div>
        </div>
      </div>
    </Layout>
  );
};

export default DashboardPage;
