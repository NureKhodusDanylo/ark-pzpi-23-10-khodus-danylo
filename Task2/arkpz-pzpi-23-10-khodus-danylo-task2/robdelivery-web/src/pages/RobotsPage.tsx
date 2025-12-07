import React from 'react';
import { motion } from 'framer-motion';
import { useQuery } from '@tanstack/react-query';
import { Bot, Battery, MapPin, Zap, Wrench } from 'lucide-react';
import Layout from '../components/Layout';
import { robotAPI } from '../lib/api';
import './RobotsPage.css';

const RobotsPage = () => {
  const { data: robots, isLoading } = useQuery({
    queryKey: ['robots'],
    queryFn: async () => {
      const response = await robotAPI.getAllRobots();
      return response.data;
    },
  });

  const getBatteryColor = (level: number) => {
    if (level >= 70) return 'success';
    if (level >= 30) return 'warning';
    return 'error';
  };

  const getStatusColor = (status: string) => {
    const colors: Record<string, string> = {
      Idle: 'success',
      Delivering: 'primary',
      Charging: 'warning',
      Maintenance: 'error',
    };
    return colors[status] || 'info';
  };

  const getStatusIcon = (status: string) => {
    const icons: Record<string, JSX.Element> = {
      Idle: <Zap size={20} />,
      Delivering: <MapPin size={20} />,
      Charging: <Battery size={20} />,
      Maintenance: <Wrench size={20} />,
    };
    return icons[status] || <Bot size={20} />;
  };

  return (
    <Layout>
      <div className="robots-page">
        <motion.div
          className="page-header"
          initial={{ opacity: 0, y: -20 }}
          animate={{ opacity: 1, y: 0 }}
        >
          <div>
            <h1 className="page-title">
              Robot <span className="gradient-text">Fleet</span>
            </h1>
            <p className="page-subtitle">Monitor and track delivery robots in real-time</p>
          </div>
        </motion.div>

        {isLoading ? (
          <div className="loading-state">
            <div className="spinner"></div>
            <p>Loading robots...</p>
          </div>
        ) : !robots || robots.length === 0 ? (
          <div className="empty-state">
            <Bot size={64} className="empty-icon" />
            <h3>No robots available</h3>
            <p>The robot fleet is currently unavailable</p>
          </div>
        ) : (
          <>
            <div className="robots-stats">
              <motion.div
                className="stat-card"
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
              >
                <div className="stat-icon stat-success">
                  <Zap size={24} />
                </div>
                <div>
                  <p className="stat-label">Available</p>
                  <h3 className="stat-value">
                    {robots.filter((r) => r.status === 'Idle').length}
                  </h3>
                </div>
              </motion.div>

              <motion.div
                className="stat-card"
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.1 }}
              >
                <div className="stat-icon stat-primary">
                  <Bot size={24} />
                </div>
                <div>
                  <p className="stat-label">Delivering</p>
                  <h3 className="stat-value">
                    {robots.filter((r) => r.status === 'Delivering').length}
                  </h3>
                </div>
              </motion.div>

              <motion.div
                className="stat-card"
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.2 }}
              >
                <div className="stat-icon stat-warning">
                  <Battery size={24} />
                </div>
                <div>
                  <p className="stat-label">Charging</p>
                  <h3 className="stat-value">
                    {robots.filter((r) => r.status === 'Charging').length}
                  </h3>
                </div>
              </motion.div>

              <motion.div
                className="stat-card"
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.3 }}
              >
                <div className="stat-icon stat-error">
                  <Wrench size={24} />
                </div>
                <div>
                  <p className="stat-label">Maintenance</p>
                  <h3 className="stat-value">
                    {robots.filter((r) => r.status === 'Maintenance').length}
                  </h3>
                </div>
              </motion.div>
            </div>

            <div className="robots-grid">
              {robots.map((robot, index) => (
                <motion.div
                  key={robot.id}
                  className="robot-card"
                  initial={{ opacity: 0, y: 20 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: index * 0.05 }}
                  whileHover={{ y: -5 }}
                >
                  <div className="robot-card-header">
                    <div className="robot-icon-wrapper">
                      <Bot size={32} className="robot-icon" />
                      <motion.div
                        className={`status-indicator status-${getStatusColor(robot.status)}`}
                        animate={{ scale: [1, 1.2, 1] }}
                        transition={{ duration: 2, repeat: Infinity }}
                      />
                    </div>
                    <span className={`badge badge-${getStatusColor(robot.status)}`}>
                      {getStatusIcon(robot.status)}
                      {robot.status}
                    </span>
                  </div>

                  <h3 className="robot-name">{robot.serialNumber}</h3>
                  <p className="robot-type">{robot.type} Robot</p>

                  <div className="robot-details">
                    <div className="detail-item">
                      <span className="detail-label">Battery</span>
                      <div className="battery-indicator">
                        <div className="battery-bar">
                          <motion.div
                            className={`battery-fill battery-${getBatteryColor(
                              robot.batteryLevel
                            )}`}
                            initial={{ width: 0 }}
                            animate={{ width: `${robot.batteryLevel}%` }}
                            transition={{ duration: 1, delay: index * 0.05 }}
                          />
                        </div>
                        <span className="battery-text">{robot.batteryLevel}%</span>
                      </div>
                    </div>

                    {robot.currentNode && (
                      <div className="detail-item">
                        <MapPin size={16} className="detail-icon" />
                        <span className="detail-text">{robot.currentNode.name}</span>
                      </div>
                    )}

                    {robot.currentLatitude && robot.currentLongitude && (
                      <div className="detail-item">
                        <span className="detail-label">Coordinates</span>
                        <span className="detail-text coordinates">
                          {robot.currentLatitude.toFixed(6)}, {robot.currentLongitude.toFixed(6)}
                        </span>
                      </div>
                    )}
                  </div>
                </motion.div>
              ))}
            </div>
          </>
        )}
      </div>
    </Layout>
  );
};

export default RobotsPage;
