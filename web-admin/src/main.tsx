import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import MasterTourPackages from './pages/MasterTourPackages'
import TravelTrends from './pages/TravelTrends'
import './index.css'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Navigate to="/catalog/packages" replace />} />
        <Route path="/catalog/packages" element={<MasterTourPackages />} />
        <Route path="/trends" element={<TravelTrends />} />
      </Routes>
    </BrowserRouter>
  </StrictMode>,
)
