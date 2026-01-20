import React from 'react'
import ReactDOM from 'react-dom/client'
import { Provider } from 'react-redux'
import { ConfigProvider, App as AntdApp } from 'antd'
import { store } from './store'
import App from './App'
import './index.css'
import viVN from 'antd/locale/vi_VN'

ReactDOM.createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
        <Provider store={store}>
            <ConfigProvider
                locale={viVN}
                theme={{
                    token: {
                        colorPrimary: '#003a8c',
                        borderRadius: 8,
                        fontFamily: 'Inter, sans-serif',
                    },
                    components: {
                        Button: {
                            colorPrimary: '#003a8c',
                        },
                        Layout: {
                            headerBg: '#ffffff',
                        }
                    }
                }}
            >
                <AntdApp>
                    <App />
                </AntdApp>
            </ConfigProvider>
        </Provider>
    </React.StrictMode>,
)
