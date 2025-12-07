import React from 'react';
import { useState } from 'react';
import { motion } from 'framer-motion';
import { useNavigate, Link } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { UserPlus, Mail, Lock, User, Phone, MapPin, Bot, Loader } from 'lucide-react';
import { authAPI } from '../lib/api';
import { useAuthStore } from '../store/authStore';
import './AuthPages.css';

const RegisterPage = () => {
  const navigate = useNavigate();
  const setAuth = useAuthStore((state) => state.setAuth);

  const [formData, setFormData] = useState({
    userName: '',
    email: '',
    password: '',
    phoneNumber: '',
    latitude: 50.4501,
    longitude: 30.5234,
    address: '',
  });

  const registerMutation = useMutation({
    mutationFn: authAPI.register,
    onSuccess: async (response) => {
      alert(response.data.message || 'Registration successful! Please login.');
      navigate('/login');
    },
    onError: (error: any) => {
      alert(error.response?.data?.message || 'Registration failed. Please try again.');
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    registerMutation.mutate(formData);
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData((prev) => ({
      ...prev,
      [e.target.name]: e.target.value,
    }));
  };

  return (
    <div className="auth-page">
      <div className="auth-background">
        <div className="gradient-orb orb-1"></div>
        <div className="gradient-orb orb-2"></div>
      </div>

      <motion.div
        className="auth-container"
        initial={{ opacity: 0, y: 30 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.6 }}
      >
        <motion.div
          className="auth-header"
          initial={{ opacity: 0, y: -20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.2 }}
        >
          <div className="auth-logo">
            <Bot size={48} className="logo-icon" />
            <span className="gradient-text">RobDelivery</span>
          </div>
          <h1 className="auth-title">Create Account</h1>
          <p className="auth-subtitle">Join the future of delivery</p>
        </motion.div>

        <motion.form
          className="auth-form"
          onSubmit={handleSubmit}
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ delay: 0.3 }}
        >
          <div className="form-group">
            <label htmlFor="userName" className="form-label">
              <User size={18} />
              Username
            </label>
            <input
              id="userName"
              name="userName"
              type="text"
              required
              value={formData.userName}
              onChange={handleChange}
              className="form-input"
              placeholder="Choose a username"
              disabled={registerMutation.isPending}
            />
          </div>

          <div className="form-group">
            <label htmlFor="email" className="form-label">
              <Mail size={18} />
              Email
            </label>
            <input
              id="email"
              name="email"
              type="email"
              required
              value={formData.email}
              onChange={handleChange}
              className="form-input"
              placeholder="your@email.com"
              disabled={registerMutation.isPending}
            />
          </div>

          <div className="form-group">
            <label htmlFor="password" className="form-label">
              <Lock size={18} />
              Password
            </label>
            <input
              id="password"
              name="password"
              type="password"
              required
              value={formData.password}
              onChange={handleChange}
              className="form-input"
              placeholder="Create a strong password"
              disabled={registerMutation.isPending}
            />
          </div>

          <div className="form-group">
            <label htmlFor="phoneNumber" className="form-label">
              <Phone size={18} />
              Phone Number
            </label>
            <input
              id="phoneNumber"
              name="phoneNumber"
              type="tel"
              required
              value={formData.phoneNumber}
              onChange={handleChange}
              className="form-input"
              placeholder="+1234567890"
              disabled={registerMutation.isPending}
            />
          </div>

          <div className="form-group">
            <label htmlFor="latitude" className="form-label">
              <MapPin size={18} />
              Latitude
            </label>
            <input
              id="latitude"
              name="latitude"
              type="number"
              step="0.000001"
              required
              value={formData.latitude}
              onChange={handleChange}
              className="form-input"
              placeholder="50.4501"
              disabled={registerMutation.isPending}
            />
          </div>

          <div className="form-group">
            <label htmlFor="longitude" className="form-label">
              <MapPin size={18} />
              Longitude
            </label>
            <input
              id="longitude"
              name="longitude"
              type="number"
              step="0.000001"
              required
              value={formData.longitude}
              onChange={handleChange}
              className="form-input"
              placeholder="30.5234"
              disabled={registerMutation.isPending}
            />
          </div>

          <div className="form-group">
            <label htmlFor="address" className="form-label">
              <MapPin size={18} />
              Address (Optional)
            </label>
            <input
              id="address"
              name="address"
              type="text"
              value={formData.address}
              onChange={handleChange}
              className="form-input"
              placeholder="Your delivery address"
              disabled={registerMutation.isPending}
            />
          </div>

          <motion.button
            type="submit"
            className="btn btn-primary btn-full"
            disabled={registerMutation.isPending}
            whileHover={{ scale: 1.02 }}
            whileTap={{ scale: 0.98 }}
          >
            {registerMutation.isPending ? (
              <>
                <Loader className="spinner-icon" size={20} />
                Creating Account...
              </>
            ) : (
              <>
                <UserPlus size={20} />
                Create Account
              </>
            )}
          </motion.button>
        </motion.form>

        <motion.div
          className="auth-footer"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ delay: 0.4 }}
        >
          <p className="auth-footer-text">
            Already have an account?{' '}
            <Link to="/login" className="auth-link">
              Sign In
            </Link>
          </p>
          <Link to="/" className="auth-link-back">
            ← Back to Home
          </Link>
        </motion.div>
      </motion.div>
    </div>
  );
};

export default RegisterPage;
