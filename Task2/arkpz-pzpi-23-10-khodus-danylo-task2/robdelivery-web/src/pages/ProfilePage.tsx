import React from 'react';
import { motion } from 'framer-motion';
import { User, Mail, Phone, MapPin, Shield } from 'lucide-react';
import Layout from '../components/Layout';
import { useAuthStore } from '../store/authStore';
import './ProfilePage.css';

const ProfilePage = () => {
  const user = useAuthStore((state) => state.user);

  return (
    <Layout>
      <div className="profile-page">
        <motion.div
          className="page-header"
          initial={{ opacity: 0, y: -20 }}
          animate={{ opacity: 1, y: 0 }}
        >
          <h1 className="page-title">
            My <span className="gradient-text">Profile</span>
          </h1>
          <p className="page-subtitle">Manage your account information</p>
        </motion.div>

        <motion.div
          className="profile-card"
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.2 }}
        >
          <div className="profile-header">
            <div className="profile-avatar-large">
              {user?.userName.charAt(0).toUpperCase()}
            </div>
            <div className="profile-info">
              <h2 className="profile-name">{user?.userName}</h2>
              <span className={`role-badge badge-${user?.role.toLowerCase()}`}>
                <Shield size={16} />
                {user?.role}
              </span>
            </div>
          </div>

          <div className="profile-details">
            <div className="detail-section">
              <h3 className="section-title">Contact Information</h3>

              <div className="detail-row">
                <div className="detail-icon">
                  <User size={20} />
                </div>
                <div className="detail-content">
                  <span className="detail-label">Username</span>
                  <span className="detail-value">{user?.userName}</span>
                </div>
              </div>

              <div className="detail-row">
                <div className="detail-icon">
                  <Mail size={20} />
                </div>
                <div className="detail-content">
                  <span className="detail-label">Email</span>
                  <span className="detail-value">{user?.email}</span>
                </div>
              </div>

              <div className="detail-row">
                <div className="detail-icon">
                  <Phone size={20} />
                </div>
                <div className="detail-content">
                  <span className="detail-label">Phone Number</span>
                  <span className="detail-value">{user?.phoneNumber}</span>
                </div>
              </div>

              <div className="detail-row">
                <div className="detail-icon">
                  <MapPin size={20} />
                </div>
                <div className="detail-content">
                  <span className="detail-label">Address</span>
                  <span className="detail-value">{user?.address}</span>
                </div>
              </div>
            </div>
          </div>
        </motion.div>
      </div>
    </Layout>
  );
};

export default ProfilePage;
