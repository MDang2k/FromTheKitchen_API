import { Route, Routes } from "react-router-dom"
import Home from "../pages/Home"
import { ROUTES } from "../utility/constants";
import Login from "../pages/auth/Login";
import Register from "../pages/auth/Register";
import OrderManagement from "../pages/order/OrderManagement";
import OrderConfirmation from "../pages/order/OrderConfirmation";
import Cart from "../pages/cart/Cart";
import Checkout from "../pages/cart/Checkout";
import MenuItemManagement from "../pages/menu/MenuItemManagement";
import MyOrders from "../pages/order/MyOrders";
import RequireAuth from "../components/layout/RequireAuth";

function AppRoutes() {
    return (
        <Routes>
            <Route path={ROUTES.HOME} element={<Home />} />
            <Route path={ROUTES.LOGIN} element={<Login />} />
            <Route path={ROUTES.REGISTER} element={<Register />} />
            <Route path={ROUTES.CART} element={<Cart />} />
            <Route path={ROUTES.CHECKOUT} element={<RequireAuth><Checkout /></RequireAuth>} />
            <Route path={ROUTES.ORDER_CONFIRMATION} element={<RequireAuth><OrderConfirmation /></RequireAuth>} />
            <Route path={ROUTES.MY_ORDERS} element={<RequireAuth><MyOrders /></RequireAuth>} />
            <Route path={ROUTES.MENU_MANAGEMENT} element={<RequireAuth admin><MenuItemManagement /></RequireAuth>} />
            <Route path={ROUTES.ORDER_MANAGEMENT} element={<RequireAuth admin><OrderManagement /></RequireAuth>} />
        </Routes>
    )
}

export default AppRoutes;
