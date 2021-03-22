import { Express } from "express-serve-static-core";
import { storage } from './localUserStorage';

class AuthRouter {
    use(app: Express) {
        app.post('/auth', (req, res) => {
            const data = req.query as { deviceId: string, platform: string };
            const authedUser = storage.newUser(data);
            console.log(`Got new user id=${authedUser._id}`);
            res.send(authedUser);
        });
        app.get('/friends/all', (req, res) => res.send([]));
        app.put('/auth', (req, res) => res.send({}));
    }
}

const auth = new AuthRouter();

export { auth };