import Footer from "./components/layout/Footer"
import Header from "./components/layout/Header"
import AppRoutes from "./routes/AppRouter"

function App() {

  return (
    <div className="flex min-h-screen flex-col">
      <Header />
      <main className="flex-1">
      <AppRoutes />
      </main>
      <Footer />
    </div>
  )
}

export default App
